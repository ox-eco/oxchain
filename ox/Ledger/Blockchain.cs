using Akka.Actor;
using Akka.Configuration;
using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Actors;
using OX.IO.Caching;
using OX.IO.Wrappers;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.Plugins;
using OX.SmartContract;
using OX.VM;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OX.Ledger
{
    public sealed class Blockchain : UntypedActor
    {
        public static UInt256 OXS => OXS_Token.Hash;
        public static UInt256 OXC => OXC_Token.Hash;
        public class ApplicationExecuted { public Transaction Transaction; public ApplicationExecutionResult[] ExecutionResults; }
        public class PersistCompleted { public Block Block; }
        public class Import { public IEnumerable<Block> Blocks; }
        public class ImportCompleted { }
        public class FillMemoryPool { public IEnumerable<Transaction> Transactions; }
        public class FillCompleted { }

        public static readonly uint SecondsPerBlock = ProtocolSettings.Default.SecondsPerBlock;
        public static readonly Fixed8 BappDetainOXS = ProtocolSettings.Default.BappDetainOXS;
        public const uint DecrementInterval = 2000000;
        public const int MaxValidators = 1024;
        public static UInt160 LockAssetContractScriptHash = UInt160.Parse("0x41a48aa8f3982151136eeeabbfa97ec9b3f56b5a");
        public static UInt160 SideAssetContractScriptHash = UInt160.Parse("0x1bb1483c8c1175b37062d7d586bd4b67abb255e2");
        public static UInt160 TrustAssetContractScriptHash = UInt160.Parse("0xe64586c07a90ec1a1b0c8fc22868cf3eff94560b");
        public static UInt160 EthereumMapContractScriptHash = UInt160.Parse("0x508c5bd9a4a5fd62ea2b0d1c853aff2cec5d5ea7");
        static readonly uint[] genesisGenerationAmount = { 100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 9, 8, 7, 6, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public static uint[] GenerationBonusAmount => genesisGenerationAmount;
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);
        public static readonly ECPoint[] StandbyValidators = ProtocolSettings.Default.StandbyValidators.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
        public static string[] StandbyValidatorAddress { get; private set; } = StandbyValidators.Select(m => Contract.CreateSignatureContract(m).Address).ToArray();
#pragma warning disable CS0612
        public static readonly RegisterTransaction OXS_Token = new RegisterTransaction
        {
            AssetType = AssetType.GoverningToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"ox股\"},{\"lang\":\"en\",\"name\":\"ox share\"}]",
            Amount = Fixed8.One * 100000000,
            Precision = 0,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHT }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Witnesses = new Witness[0]
        };

        public static readonly RegisterTransaction OXC_Token = new RegisterTransaction
        {
            AssetType = AssetType.UtilityToken,
            Name = "[{\"lang\":\"zh-CN\",\"name\":\"ox币\"},{\"lang\":\"en\",\"name\":\"ox coin\"}]",
            Amount = -Fixed8.Satoshi,//Fixed8.FromDecimal(GenerationAmount.Sum(p => p) * DecrementInterval),
            Precision = 8,
            Owner = ECCurve.Secp256r1.Infinity,
            Admin = (new[] { (byte)OpCode.PUSHF }).ToScriptHash(),
            Attributes = new TransactionAttribute[0],
            Inputs = new CoinReference[0],
            Outputs = new TransactionOutput[0],
            Witnesses = new Witness[0]
        };
#pragma warning restore CS0612

        public static readonly Block GenesisBlock = new Block
        {
            PrevHash = UInt256.Zero,
            Timestamp = (new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc)).ToTimestamp(),
            Index = 0,
            ConsensusData = 201014021116, //for my love
            NextConsensus = GetConsensusAddress(StandbyValidators),
            Witness = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            },
            Transactions = new Transaction[]
            {
                new MinerTransaction
                {
                    Nonce = 788289,
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new TransactionOutput[0],
                    Witnesses = new Witness[0]
                },
                OXS_Token,
                OXC_Token,
                new IssueTransaction
                {
                    Attributes = new TransactionAttribute[0],
                    Inputs = new CoinReference[0],
                    Outputs = new[]
                    {
                        new TransactionOutput
                        {
                            AssetId = OXS_Token.Hash,
                            Value = OXS_Token.Amount,
                            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyValidators.Length / 2 + 1, StandbyValidators).ToScriptHash()
                        },
                        new TransactionOutput
                        {
                            AssetId = OXC_Token.Hash,
                            Value = Fixed8.One* 40000000,
                            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyValidators.Length / 2 + 1, StandbyValidators).ToScriptHash()
                        }
                    },
                    Witnesses = new[]
                    {
                        new Witness
                        {
                            InvocationScript = new byte[0],
                            VerificationScript = new[] { (byte)OpCode.PUSHT }
                        }
                    }
                }
            }
        };
        public static bool Debug = false;
        private const int MemoryPoolMaxTransactions = 50_000;
        private const int MaxTxToReverifyPerIdle = 10;
        private static readonly object lockObj = new object();
        private readonly OXSystem system;
        private readonly List<UInt256> header_index = new List<UInt256>();
        private uint stored_header_count = 0;
        private readonly Dictionary<UInt256, Block> block_cache = new Dictionary<UInt256, Block>();
        private readonly Dictionary<uint, LinkedList<Block>> block_cache_unverified = new Dictionary<uint, LinkedList<Block>>();
        internal readonly RelayCache RelayCache = new RelayCache(100);
        private Snapshot currentSnapshot;
        public Snapshot CurrentSnapshot { get { return currentSnapshot; } }
        public Store Store { get; }
        public MemoryPool MemPool { get; }
        public FlashStatePool StatePool { get; }
        public uint Height => currentSnapshot.Height;

        public uint HeaderHeight => currentSnapshot.HeaderHeight;
        public UInt256 CurrentBlockHash => currentSnapshot.CurrentBlockHash;
        public UInt256 CurrentHeaderHash => currentSnapshot.CurrentHeaderHash;

        private static Blockchain singleton;
        public static Blockchain Singleton
        {
            get
            {
                while (singleton == null) Thread.Sleep(10);
                return singleton;
            }
        }

        static Blockchain()
        {
            GenesisBlock.RebuildMerkleRoot();
        }

        public Blockchain(OXSystem system, Store store)
        {
            this.system = system;
            this.MemPool = new MemoryPool(system, MemoryPoolMaxTransactions);
            this.StatePool = new FlashStatePool(system);
            this.Store = store;
            lock (lockObj)
            {
                if (singleton != null)
                    throw new InvalidOperationException();
                header_index.AddRange(store.GetHeaderHashList().Find().OrderBy(p => (uint)p.Key).SelectMany(p => p.Value.Hashes));
                stored_header_count += (uint)header_index.Count;
                if (stored_header_count == 0)
                {
                    header_index.AddRange(store.GetBlocks().Find().OrderBy(p => p.Value.TrimmedBlock.Index).Select(p => p.Key));
                }
                else
                {
                    HashIndexState hashIndex = store.GetHeaderHashIndex().Get();
                    if (hashIndex.Index >= stored_header_count)
                    {
                        DataCache<UInt256, BlockState> cache = store.GetBlocks();
                        for (UInt256 hash = hashIndex.Hash; hash != header_index[(int)stored_header_count - 1];)
                        {
                            header_index.Insert((int)stored_header_count, hash);
                            hash = cache[hash].TrimmedBlock.PrevHash;
                        }
                    }
                }
                if (header_index.Count == 0)
                    Persist(GenesisBlock);
                else
                    UpdateCurrentSnapshot();
                singleton = this;
            }
        }

        public bool ContainsBlock(UInt256 hash)
        {
            if (block_cache.ContainsKey(hash)) return true;
            return Store.ContainsBlock(hash);
        }

        public bool ContainsTransaction(UInt256 hash)
        {
            if (MemPool.ContainsKey(hash)) return true;
            return Store.ContainsTransaction(hash);
        }

        public Block GetBlock(UInt256 hash)
        {
            if (block_cache.TryGetValue(hash, out Block block))
                return block;
            return Store.GetBlock(hash);
        }

        public UInt256 GetBlockHash(uint index)
        {
            if (header_index.Count <= index) return null;
            return header_index[(int)index];
        }

        public static UInt160 GetConsensusAddress(ECPoint[] validators)
        {
            return Contract.CreateMultiSigRedeemScript(validators.Length - (validators.Length - 1) / 3, validators).ToScriptHash();
        }

        public Snapshot GetSnapshot()
        {
            return Store.GetSnapshot();
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            if (MemPool.TryGetValue(hash, out Transaction transaction))
                return transaction;
            return Store.GetTransaction(hash);
        }

        private void OnImport(IEnumerable<Block> blocks)
        {
            foreach (Block block in blocks)
            {
                if (block.Index <= Height) continue;
                if (block.Index != Height + 1)
                    throw new InvalidOperationException();
                Persist(block);
                SaveHeaderHashList();
            }
            Sender.Tell(new ImportCompleted());
        }

        private void AddUnverifiedBlockToCache(Block block)
        {
            if (!block_cache_unverified.TryGetValue(block.Index, out LinkedList<Block> blocks))
            {
                blocks = new LinkedList<Block>();
                block_cache_unverified.Add(block.Index, blocks);
            }

            blocks.AddLast(block);
        }

        private void OnFillMemoryPool(IEnumerable<Transaction> transactions)
        {
            // Invalidate all the transactions in the memory pool, to avoid any failures when adding new transactions.
            MemPool.InvalidateAllTransactions();

            // Add the transactions to the memory pool
            foreach (var tx in transactions)
            {
                if (tx.Type == TransactionType.MinerTransaction)
                    continue;
                if (Store.ContainsTransaction(tx.Hash))
                    continue;
                if (!Plugin.CheckPolicy(tx))
                    continue;
                // First remove the tx if it is unverified in the pool.
                MemPool.TryRemoveUnVerified(tx.Hash, out _);
                // Verify the the transaction
                if (!tx.Verify(currentSnapshot, MemPool.GetVerifiedTransactions()))
                    continue;
                // Add to the memory pool
                MemPool.TryAdd(tx.Hash, tx);
            }
            // Transactions originally in the pool will automatically be reverified based on their priority.

            Sender.Tell(new FillCompleted());
        }

        private RelayResultReason OnNewBlock(Block block)
        {
            if (block.Index <= Height)
                return RelayResultReason.AlreadyExists;
            if (block_cache.ContainsKey(block.Hash))
                return RelayResultReason.AlreadyExists;
            if (block.Index - 1 >= header_index.Count)
            {
                AddUnverifiedBlockToCache(block);
                return RelayResultReason.UnableToVerify;
            }
            if (block.Index == header_index.Count)
            {
                if (!block.Verify(currentSnapshot))
                    return RelayResultReason.Invalid;
            }
            else
            {
                if (!block.Hash.Equals(header_index[(int)block.Index]))
                    return RelayResultReason.Invalid;
            }
            if (block.Index == Height + 1)
            {
                Block block_persist = block;
                List<Block> blocksToPersistList = new List<Block>();
                while (true)
                {
                    blocksToPersistList.Add(block_persist);
                    if (block_persist.Index + 1 >= header_index.Count) break;
                    UInt256 hash = header_index[(int)block_persist.Index + 1];
                    if (!block_cache.TryGetValue(hash, out block_persist)) break;
                }

                int blocksPersisted = 0;
                foreach (Block blockToPersist in blocksToPersistList)
                {
                    block_cache_unverified.Remove(blockToPersist.Index);
                    Persist(blockToPersist);

                    if (blocksPersisted++ < blocksToPersistList.Count - (2 + Math.Max(0, (15 - SecondsPerBlock)))) continue;
                    // Empirically calibrated for relaying the most recent 2 blocks persisted with 15s network
                    // Increase in the rate of 1 block per second in configurations with faster blocks

                    if (blockToPersist.Index + 100 >= header_index.Count)
                        system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = blockToPersist });
                }
                SaveHeaderHashList();

                if (block_cache_unverified.TryGetValue(Height + 1, out LinkedList<Block> unverifiedBlocks))
                {
                    foreach (var unverifiedBlock in unverifiedBlocks)
                        Self.Tell(unverifiedBlock, ActorRefs.NoSender);
                    block_cache_unverified.Remove(Height + 1);
                }
            }
            else
            {
                block_cache.Add(block.Hash, block);
                if (block.Index + 100 >= header_index.Count)
                    system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = block });
                if (block.Index == header_index.Count)
                {
                    header_index.Add(block.Hash);
                    using (Snapshot snapshot = GetSnapshot())
                    {
                        snapshot.Blocks.Add(block.Hash, new BlockState
                        {
                            SystemFeeAmount = 0,
                            TrimmedBlock = block.Header.Trim()
                        });
                        snapshot.HeaderHashIndex.GetAndChange().Hash = block.Hash;
                        snapshot.HeaderHashIndex.GetAndChange().Index = block.Index;
                        SaveHeaderHashList(snapshot);
                        snapshot.Commit();
                    }
                    UpdateCurrentSnapshot();
                }
            }
            return RelayResultReason.Succeed;
        }

        private RelayResultReason OnNewConsensus(ConsensusPayload payload)
        {
            if (!payload.Verify(currentSnapshot)) return RelayResultReason.Invalid;
            system.Consensus?.Tell(payload);
            RelayCache.Add(payload);
            system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = payload });
            return RelayResultReason.Succeed;
        }

        private void OnNewHeaders(Header[] headers)
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                foreach (Header header in headers)
                {
                    if (header.Index - 1 >= header_index.Count) break;
                    if (header.Index < header_index.Count) continue;
                    if (!header.Verify(snapshot)) break;
                    header_index.Add(header.Hash);
                    snapshot.Blocks.Add(header.Hash, new BlockState
                    {
                        SystemFeeAmount = 0,
                        TrimmedBlock = header.Trim()
                    });
                    snapshot.HeaderHashIndex.GetAndChange().Hash = header.Hash;
                    snapshot.HeaderHashIndex.GetAndChange().Index = header.Index;
                }
                SaveHeaderHashList(snapshot);
                snapshot.Commit();
            }
            UpdateCurrentSnapshot();
            system.TaskManager.Tell(new TaskManager.HeaderTaskCompleted(), Sender);
        }

        private RelayResultReason OnNewTransaction(Transaction transaction)
        {
            if (transaction.Type == TransactionType.MinerTransaction)
                return RelayResultReason.Invalid;
            if (ContainsTransaction(transaction.Hash))
                return RelayResultReason.AlreadyExists;
            if (!MemPool.CanTransactionFitInPool(transaction))
                return RelayResultReason.OutOfMemory;
            if (!transaction.Verify(currentSnapshot, MemPool.GetVerifiedTransactions()))
                return RelayResultReason.Invalid;
            if (!Plugin.CheckPolicy(transaction))
                return RelayResultReason.PolicyFail;

            if (!MemPool.TryAdd(transaction.Hash, transaction))
                return RelayResultReason.OutOfMemory;

            system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = transaction });
            return RelayResultReason.Succeed;
        }

        private void OnPersistCompleted(Block block)
        {
            Debugger.Log(nameof(Blockchain), $"block persist completed=>{block.Index}/{block.Hash}", 2);
            block_cache.Remove(block.Hash);
            MemPool.UpdatePoolForBlockPersisted(block, currentSnapshot);
            Context.System.EventStream.Publish(new PersistCompleted { Block = block });
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Import import:
                    OnImport(import.Blocks);
                    break;
                case FillMemoryPool fill:
                    OnFillMemoryPool(fill.Transactions);
                    break;
                case Header[] headers:
                    OnNewHeaders(headers);
                    break;
                case Block block:
                    Sender.Tell(OnNewBlock(block));
                    break;
                case Transaction transaction:
                    Sender.Tell(OnNewTransaction(transaction));
                    break;
                case ConsensusPayload payload:
                    Sender.Tell(OnNewConsensus(payload));
                    break;
                case Idle _:
                    if (MemPool.ReVerifyTopUnverifiedTransactionsIfNeeded(MaxTxToReverifyPerIdle, currentSnapshot))
                        Self.Tell(Idle.Instance, ActorRefs.NoSender);
                    break;
            }
        }

        private void Persist(Block block)
        {
            using (Snapshot snapshot = GetSnapshot())
            {
                List<ApplicationExecuted> all_application_executed = new List<ApplicationExecuted>();
                snapshot.PersistingBlock = block;
                snapshot.Blocks.Add(block.Hash, new BlockState
                {
                    SystemFeeAmount = snapshot.GetSysFeeAmount(block.PrevHash) + (long)block.Transactions.Sum(p => p.SystemFee),
                    TrimmedBlock = block.Trim()
                });
                ushort n = 0;
                foreach (Transaction tx in block.Transactions)
                {
                    snapshot.Transactions.Add(tx.Hash, new TransactionState
                    {
                        BlockIndex = block.Index,
                        Transaction = tx
                    });
                    snapshot.UnspentCoins.Add(tx.Hash, new UnspentCoinState
                    {
                        Items = Enumerable.Repeat(CoinState.Confirmed, tx.Outputs.Length).ToArray()
                    });
                    foreach (TransactionOutput output in tx.Outputs)
                    {
                        AccountState account = snapshot.Accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
                        if (account.Balances.ContainsKey(output.AssetId))
                            account.Balances[output.AssetId] += output.Value;
                        else
                            account.Balances[output.AssetId] = output.Value;
                        if (output.AssetId.Equals(OXS_Token.Hash) && account.Votes.Length > 0)
                        {
                            foreach (ECPoint pubkey in account.Votes)
                                snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
                            snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
                        }
                    }
                    foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
                    {
                        TransactionState tx_prev = snapshot.Transactions[group.Key];
                        foreach (CoinReference input in group)
                        {
                            snapshot.UnspentCoins.GetAndChange(input.PrevHash).Items[input.PrevIndex] |= CoinState.Spent;
                            TransactionOutput out_prev = tx_prev.Transaction.Outputs[input.PrevIndex];
                            AccountState account = snapshot.Accounts.GetAndChange(out_prev.ScriptHash);
                            if (out_prev.AssetId.Equals(OXS_Token.Hash))
                            {
                                snapshot.SpentCoins.GetAndChange(input.PrevHash, () => new SpentCoinState
                                {
                                    TransactionHash = input.PrevHash,
                                    TransactionHeight = tx_prev.BlockIndex,
                                    Items = new Dictionary<ushort, uint>()
                                }).Items.Add(input.PrevIndex, block.Index);
                                if (account.Votes.Length > 0)
                                {
                                    foreach (ECPoint pubkey in account.Votes)
                                    {
                                        ValidatorState validator = snapshot.Validators.GetAndChange(pubkey);
                                        validator.Votes -= out_prev.Value;
                                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                                            snapshot.Validators.Delete(pubkey);
                                    }
                                    snapshot.ValidatorsCount.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
                                }
                            }
                            account.Balances[out_prev.AssetId] -= out_prev.Value;
                        }
                    }
                    List<ApplicationExecutionResult> execution_results = new List<ApplicationExecutionResult>();
                    switch (tx)
                    {
#pragma warning disable CS0612
                        case RegisterTransaction tx_register:
                            snapshot.Assets.Add(tx.Hash, new AssetState
                            {
                                AssetId = tx_register.Hash,
                                AssetType = tx_register.AssetType,
                                Name = tx_register.Name,
                                Amount = tx_register.Amount,
                                Available = Fixed8.Zero,
                                Precision = tx_register.Precision,
                                Fee = Fixed8.Zero,
                                FeeAddress = new UInt160(),
                                Owner = tx_register.Owner,
                                Admin = tx_register.Admin,
                                Issuer = tx_register.Admin,
                                Expiration = block.Index + 2 * 2000000,
                                IsFrozen = false
                            });
                            break;
#pragma warning restore CS0612
                        case IssueTransaction _:
                            foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                                snapshot.Assets.GetAndChange(result.AssetId).Available -= result.Amount;
                            break;
                        case EthereumMapTransaction _:
                            foreach (TransactionResult result in tx.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                                snapshot.Assets.GetAndChange(result.AssetId).Available -= result.Amount;
                            break;
                        case LockAssetTransaction lat:
                            foreach (TransactionResult result in lat.GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
                                snapshot.Assets.GetAndChange(result.AssetId).Available -= result.Amount;
                            if (lat.LockContract.Equals(LockAssetContractScriptHash) && lat.ValidBlockBonusVote(out BlockBonusSetting bonusSetting, out Fixed8 voteAmount))
                            {
                                var blockBonusVote = new BlockBonusVote { Amount = voteAmount, Voter = lat.Recipient, NumPerBlock = bonusSetting.NumPerBlock };
                                var bonusVoteList = snapshot.BlockBonusVoteList.TryGet((UInt32Wrapper)bonusSetting.Index);
                                if (bonusVoteList.IsNotNull())
                                {
                                    var list = bonusVoteList.Votes.ToList();
                                    list.Add(blockBonusVote);
                                    bonusVoteList.Votes = list.ToArray();
                                    snapshot.BlockBonusVoteList.GetAndChange((UInt32Wrapper)bonusSetting.Index, () => bonusVoteList);
                                }
                                else
                                {
                                    BlockBonusVoteList blockBonusVoteList = new BlockBonusVoteList { Votes = new BlockBonusVote[] { blockBonusVote } };
                                    snapshot.BlockBonusVoteList.Add((UInt32Wrapper)bonusSetting.Index, blockBonusVoteList);
                                }
                            }
                            break;
                        case ClaimTransaction _:
                            foreach (CoinReference input in ((ClaimTransaction)tx).Claims)
                            {
                                if (snapshot.SpentCoins.TryGet(input.PrevHash)?.Items.Remove(input.PrevIndex) == true)
                                    snapshot.SpentCoins.GetAndChange(input.PrevHash);
                            }
                            break;
                        //#pragma warning disable CS0612
                        //                        case EnrollmentTransaction tx_enrollment:
                        //                            snapshot.Validators.GetAndChange(tx_enrollment.PublicKey, () => new ValidatorState(tx_enrollment.PublicKey)).Registered = true;
                        //                            break;
                        //#pragma warning restore CS0612
                        case StateTransaction tx_state:
                            foreach (StateDescriptor descriptor in tx_state.Descriptors)
                                switch (descriptor.Type)
                                {
                                    case StateType.Account:
                                        ProcessAccountStateDescriptor(descriptor, snapshot);
                                        break;
                                    case StateType.Validator:
                                        ProcessValidatorStateDescriptor(descriptor, snapshot);
                                        break;
                                }
                            break;
                        //#pragma warning disable CS0612
                        //                        case PublishTransaction tx_publish:
                        //                            snapshot.Contracts.GetOrAdd(tx_publish.ScriptHash, () => new ContractState
                        //                            {
                        //                                Script = tx_publish.Script,
                        //                                ParameterList = tx_publish.ParameterList,
                        //                                ReturnType = tx_publish.ReturnType,
                        //                                ContractProperties = (ContractPropertyState)Convert.ToByte(tx_publish.NeedStorage),
                        //                                Name = tx_publish.Name,
                        //                                CodeVersion = tx_publish.CodeVersion,
                        //                                Author = tx_publish.Author,
                        //                                Email = tx_publish.Email,
                        //                                Description = tx_publish.Description
                        //                            });
                        //                            break;
                        //#pragma warning restore CS0612
                        case InvocationTransaction tx_invocation:
                            using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, tx_invocation, snapshot.Clone(), tx_invocation.Gas))
                            {
                                engine.LoadScript(tx_invocation.Script);
                                engine.Execute();
                                if (!engine.State.HasFlag(VMState.FAULT))
                                {
                                    engine.Service.Commit();
                                }
                                execution_results.Add(new ApplicationExecutionResult
                                {
                                    Trigger = TriggerType.Application,
                                    ScriptHash = tx_invocation.Script.ToScriptHash(),
                                    VMState = engine.State,
                                    GasConsumed = engine.GasConsumed,
                                    Stack = engine.ResultStack.ToArray(),
                                    Notifications = engine.Service.Notifications.ToArray()
                                });
                            }
                            break;

                        case DetainTransaction tx_detain:
                            switch (tx_detain.DetainState)
                            {
                                case DetainStatus.Freeze:
                                    var sh = tx_detain.ScriptHash;
                                    var accountState = new AccountState(sh) { DetainState = tx_detain.DetainState, DetainExpire = block.Index, AskFee = tx_detain.AskFee };
                                    AccountState acts = snapshot.Accounts.GetAndChange(sh, () => accountState);
                                    var expire = acts.DetainExpire;
                                    if (expire < block.Index)
                                        expire = block.Index;
                                    expire += tx_detain.DetainDuration;
                                    acts.AskFee = tx_detain.AskFee;
                                    acts.DetainState = tx_detain.DetainState;
                                    acts.DetainExpire = expire;
                                    break;
                                case DetainStatus.UnFreeze:
                                    var sh2 = tx_detain.ScriptHash;
                                    AccountState acts2 = snapshot.Accounts.GetAndChange(sh2, () => null);
                                    if (acts2.IsNotNull())
                                    {
                                        if (acts2.DetainExpire < block.Index)
                                        {
                                            acts2.DetainState = DetainStatus.UnFreeze;
                                            acts2.DetainExpire = 0;
                                        }
                                    }
                                    break;
                            }
                            break;
                        case SideTransaction tx_side:
                            var recipientScriptHash = Contract.CreateSignatureRedeemScript(tx_side.Recipient).ToScriptHash();
                            var sideState = new SideState { SideScriptHash = tx_side.GetContract().ScriptHash, SideTransaction = tx_side };
                            var sideList = snapshot.Sides.TryGet(recipientScriptHash);
                            if (sideList.IsNotNull())
                            {
                                var list = sideList.SideStateList.ToList();
                                list.Add(sideState);
                                sideList.SideStateList = list.ToArray();
                                snapshot.Sides.GetAndChange(recipientScriptHash, () => sideList);
                            }
                            else
                            {
                                SideSateList sideStateList = new SideSateList { SideStateList = new SideState[] { sideState } };
                                snapshot.Sides.Add(recipientScriptHash, sideStateList);
                            }
                            break;
                        case NftTransaction tx_nfc:
                            snapshot.NFTs.Add(tx_nfc.NftCopyright.NftID, new NFCState { NFC = tx_nfc, BlockIndex = block.Index, N = n });
                            break;
                        case NftTransferTransaction tx_nfs:
                            var nft = snapshot.NFTs.TryGet(tx_nfs.NFSStateKey.NFCID);
                            if (nft.IsNotNull())
                            {
                                switch (tx_nfs.NftChangeType)
                                {
                                    case NftChangeType.Issue:
                                        NFSStateKey nfskey = new NFSStateKey { NFCID = tx_nfs.NFSStateKey.NFCID, IssueBlockIndex = block.Index, IssueN = n };
                                        snapshot.NFTTransfers.Add(nfskey, new NFSState { LastNFS = tx_nfs, TransferBlockIndex = 0, TransferN = 0 });
                                        nft.TotalIssue++;
                                        snapshot.NFTs.GetAndChange(tx_nfs.NFSStateKey.NFCID, () => nft);
                                        break;
                                    case NftChangeType.Transfer:
                                        var nfsState = snapshot.NFTTransfers.TryGet(tx_nfs.NFSStateKey);
                                        if (nfsState.IsNotNull())
                                        {
                                            nfsState.LastNFS = tx_nfs;
                                            nfsState.TransferBlockIndex = block.Index;
                                            nfsState.TransferN = n;
                                            snapshot.NFTTransfers.GetAndChange(tx_nfs.NFSStateKey, () => nfsState);
                                        }
                                        nft.TotalTransfer++;
                                        nft.TotalAmountTransfer += tx_nfs.Auth.Target.Amount;
                                        snapshot.NFTs.GetAndChange(tx_nfs.NFSStateKey.NFCID, () => nft);
                                        break;
                                }
                            }
                            break;
                        case BookTransaction tx_book:
                            snapshot.Books.Add(tx.Hash, new BookState { Book = tx_book, CopyrightOwner = Contract.CreateSignatureRedeemScript(tx_book.Author).ToScriptHash(), BlockIndex = block.Index, N = n, DataHash = tx_book.Hash });
                            break;
                        case BookSectionTransaction tx_booksection:
                            BookState bookState = snapshot.Books.GetAndChange(tx_booksection.BookId, () => null);
                            if (bookState.IsNotNull())
                            {
                                bookState.Sections[tx_booksection.FixedSerial] = tx_booksection.Hash;
                            }
                            break;
                        case BookTransferTransaction tx_booktransfer:
                            BookState bookState2 = snapshot.Books.GetAndChange(tx_booktransfer.BookCopyrightAuthentication.Target.BookId, () => null);
                            if (bookState2.IsNotNull())
                            {
                                bookState2.CopyrightOwner = tx_booktransfer.Owner;
                            }
                            break;
                    }
                    if (execution_results.Count > 0)
                    {
                        ApplicationExecuted application_executed = new ApplicationExecuted
                        {
                            Transaction = tx,
                            ExecutionResults = execution_results.ToArray()
                        };
                        Context.System.EventStream.Publish(application_executed);
                        all_application_executed.Add(application_executed);
                    }
                    n++;
                }
                snapshot.BlockHashIndex.GetAndChange().Hash = block.Hash;
                snapshot.BlockHashIndex.GetAndChange().Index = block.Index;
                if (block.Index == header_index.Count)
                {
                    header_index.Add(block.Hash);
                    snapshot.HeaderHashIndex.GetAndChange().Hash = block.Hash;
                    snapshot.HeaderHashIndex.GetAndChange().Index = block.Index;
                }
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                    plugin.OnPersist(snapshot, all_application_executed);
                snapshot.Commit();
                List<Exception> commitExceptions = null;
                foreach (IPersistencePlugin plugin in Plugin.PersistencePlugins)
                {
                    try
                    {
                        plugin.OnCommit(snapshot);
                    }
                    catch (Exception ex)
                    {
                        if (plugin.ShouldThrowExceptionFromCommit(ex))
                        {
                            if (commitExceptions == null)
                                commitExceptions = new List<Exception>();

                            commitExceptions.Add(ex);
                        }
                    }
                }
                if (commitExceptions != null) throw new AggregateException(commitExceptions);
            }
            UpdateCurrentSnapshot();
            OnPersistCompleted(block);
        }

        protected override void PostStop()
        {
            base.PostStop();
            currentSnapshot?.Dispose();
        }

        internal static void ProcessAccountStateDescriptor(StateDescriptor descriptor, Snapshot snapshot)
        {
            UInt160 hash = new UInt160(descriptor.Key);
            AccountState account = snapshot.Accounts.GetAndChange(hash, () => new AccountState(hash));
            switch (descriptor.Field)
            {
                case "Votes":
                    Fixed8 balance = account.GetBalance(OXS_Token.Hash);
                    foreach (ECPoint pubkey in account.Votes)
                    {
                        ValidatorState validator = snapshot.Validators.GetAndChange(pubkey);
                        validator.Votes -= balance;
                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
                            snapshot.Validators.Delete(pubkey);
                    }
                    ECPoint[] votes = descriptor.Value.AsSerializableArray<ECPoint>().Distinct().ToArray();
                    if (votes.Length != account.Votes.Length)
                    {
                        ValidatorsCountState count_state = snapshot.ValidatorsCount.GetAndChange();
                        if (account.Votes.Length > 0)
                            count_state.Votes[account.Votes.Length - 1] -= balance;
                        if (votes.Length > 0)
                            count_state.Votes[votes.Length - 1] += balance;
                    }
                    account.Votes = votes;
                    foreach (ECPoint pubkey in account.Votes)
                        snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += balance;
                    break;
            }
        }

        internal static void ProcessValidatorStateDescriptor(StateDescriptor descriptor, Snapshot snapshot)
        {
            ECPoint pubkey = ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1);
            ValidatorState validator = snapshot.Validators.GetAndChange(pubkey, () => new ValidatorState(pubkey));
            switch (descriptor.Field)
            {
                case "Registered":
                    validator.Registered = BitConverter.ToBoolean(descriptor.Value, 0);
                    break;
            }
        }

        public static Props Props(OXSystem system, Store store)
        {
            return Akka.Actor.Props.Create(() => new Blockchain(system, store)).WithMailbox("blockchain-mailbox");
        }

        private void SaveHeaderHashList(Snapshot snapshot = null)
        {
            if ((header_index.Count - stored_header_count < 2000))
                return;
            bool snapshot_created = snapshot == null;
            if (snapshot_created) snapshot = GetSnapshot();
            try
            {
                while (header_index.Count - stored_header_count >= 2000)
                {
                    snapshot.HeaderHashList.Add(stored_header_count, new HeaderHashList
                    {
                        Hashes = header_index.Skip((int)stored_header_count).Take(2000).ToArray()
                    });
                    stored_header_count += 2000;
                }
                if (snapshot_created) snapshot.Commit();
            }
            finally
            {
                if (snapshot_created) snapshot.Dispose();
            }
        }

        private void UpdateCurrentSnapshot()
        {
            Interlocked.Exchange(ref currentSnapshot, GetSnapshot())?.Dispose();
        }
        public bool VerifyBizValidator(UInt160 bizValidatorScriptHash, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            return VerifyBizValidator(currentSnapshot, bizValidatorScriptHash, out OXSBalance, out AskFee);
        }
        public bool VerifyBizValidator(Snapshot snapshot, UInt160 bizValidatorScriptHash, out Fixed8 OXSBalance, out Fixed8 AskFee)
        {
            OXSBalance = Fixed8.Zero;
            AskFee = Fixed8.Zero;
            var acts = snapshot.Accounts.GetAndChange(bizValidatorScriptHash, () => null);
            if (acts.IsNull()) return false;
            var balance = acts.GetBalance(OXS);
            OXSBalance = balance;
            AskFee = acts.AskFee;
            if (acts.DetainState == DetainStatus.UnFreeze) return false;
            if (acts.DetainExpire < currentSnapshot.Height) return false;
            if (balance < BappDetainOXS) return false;
            return true;
        }
        public bool IsFrozen(UInt160 scriptHash, out uint ExpireIndex)
        {
            var acts = currentSnapshot.Accounts.GetAndChange(scriptHash, () => null);
            if (acts.IsNull())
            {
                ExpireIndex = 0;
                return false;
            }

            bool isFrozen = acts.DetainState == DetainStatus.Freeze;
            if (isFrozen)
                ExpireIndex = acts.DetainExpire;
            else
                ExpireIndex = 0;
            return isFrozen;
        }
    }

    internal class BlockchainMailbox : PriorityMailbox
    {
        public BlockchainMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        internal protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case Header[] _:
                case Block _:
                case ConsensusPayload _:
                case CenterTransaction _:
                case Terminated _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
