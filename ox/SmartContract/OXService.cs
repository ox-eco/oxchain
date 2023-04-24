using Akka.Util;
using OX.Cryptography.ECC;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.SmartContract.Enumerators;
using OX.SmartContract.Iterators;
using OX.VM;
using OX.VM.Types;
using OX.Wallets;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using VMArray = OX.VM.Types.Array;

namespace OX.SmartContract
{
    public class OXService : StandardService
    {
        public OXService(TriggerType trigger, Snapshot snapshot)
            : base(trigger, snapshot)
        {
            Register("OX.Runtime.GetTrigger", Runtime_GetTrigger, 1);
            Register("OX.Runtime.CheckWitness", Runtime_CheckWitness, 200);
            Register("OX.Runtime.Notify", Runtime_Notify, 1);
            Register("OX.Runtime.Log", Runtime_Log, 1);
            Register("OX.Runtime.GetTime", Runtime_GetTime, 1);
            Register("OX.Runtime.Serialize", Runtime_Serialize, 1);
            Register("OX.Runtime.Deserialize", Runtime_Deserialize, 1);
            Register("OX.Blockchain.GetHeight", Blockchain_GetHeight, 1);
            Register("OX.Blockchain.GetHeader", Blockchain_GetHeader, 100);
            Register("OX.Blockchain.GetBlock", Blockchain_GetBlock, 200);
            Register("OX.Blockchain.GetTransaction", Blockchain_GetTransaction, 100);
            Register("OX.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight, 100);
            Register("OX.Blockchain.GetAccount", Blockchain_GetAccount, 100);
            Register("OX.Blockchain.GetValidators", Blockchain_GetValidators, 200);
            Register("OX.Blockchain.GetAsset", Blockchain_GetAsset, 100);
            Register("OX.Blockchain.GetContract", Blockchain_GetContract, 100);
            Register("OX.Header.GetHash", Header_GetHash, 1);
            Register("OX.Header.GetVersion", Header_GetVersion, 1);
            Register("OX.Header.GetPrevHash", Header_GetPrevHash, 1);
            Register("OX.Header.GetMerkleRoot", Header_GetMerkleRoot, 1);
            Register("OX.Header.GetTimestamp", Header_GetTimestamp, 1);
            Register("OX.Header.GetIndex", Header_GetIndex, 1);
            Register("OX.Header.GetConsensusData", Header_GetConsensusData, 1);
            Register("OX.Header.GetNextConsensus", Header_GetNextConsensus, 1);
            Register("OX.Block.GetTransactionCount", Block_GetTransactionCount, 1);
            Register("OX.Block.GetTransactions", Block_GetTransactions, 1);
            Register("OX.Block.GetTransaction", Block_GetTransaction, 1);
            Register("OX.Transaction.GetHash", Transaction_GetHash, 1);
            Register("OX.Transaction.GetInputHash", Transaction_GetInputHash, 1);
            Register("OX.Transaction.GetType", Transaction_GetType, 1);
            Register("OX.Transaction.GetAttributes", Transaction_GetAttributes, 1);
            Register("OX.Transaction.GetInputs", Transaction_GetInputs, 1);
            Register("OX.Transaction.GetOutputs", Transaction_GetOutputs, 1);
            Register("OX.Transaction.GetReferences", Transaction_GetReferences, 200);
            Register("OX.Transaction.GetUnspentCoins", Transaction_GetUnspentCoins, 200);
            Register("OX.Transaction.GetWitnesses", Transaction_GetWitnesses, 200);
            Register("OX.InvocationTransaction.GetScript", InvocationTransaction_GetScript, 1);
            Register("OX.Witness.GetVerificationScript", Witness_GetVerificationScript, 100);
            Register("OX.Attribute.GetUsage", Attribute_GetUsage, 1);
            Register("OX.Attribute.GetData", Attribute_GetData, 1);
            Register("OX.Input.GetHash", Input_GetHash, 1);
            Register("OX.Input.GetIndex", Input_GetIndex, 1);
            Register("OX.Output.GetAssetId", Output_GetAssetId, 1);
            Register("OX.Output.GetValue", Output_GetValue, 1);
            Register("OX.Output.GetScriptHash", Output_GetScriptHash, 1);
            Register("OX.Account.GetScriptHash", Account_GetScriptHash, 1);
            Register("OX.Account.GetVotes", Account_GetVotes, 1);
            Register("OX.Account.GetBalance", Account_GetBalance, 1);
            Register("OX.Account.IsStandard", Account_IsStandard, 100);
            Register("OX.Asset.Create", Asset_Create);
            Register("OX.Asset.Renew", Asset_Renew);
            Register("OX.Asset.GetAssetId", Asset_GetAssetId, 1);
            Register("OX.Asset.GetAssetType", Asset_GetAssetType, 1);
            Register("OX.Asset.GetAmount", Asset_GetAmount, 1);
            Register("OX.Asset.GetAvailable", Asset_GetAvailable, 1);
            Register("OX.Asset.GetPrecision", Asset_GetPrecision, 1);
            Register("OX.Asset.GetOwner", Asset_GetOwner, 1);
            Register("OX.Asset.GetAdmin", Asset_GetAdmin, 1);
            Register("OX.Asset.GetIssuer", Asset_GetIssuer, 1);
            Register("OX.Contract.Create", Contract_Create);
            Register("OX.Contract.Migrate", Contract_Migrate);
            Register("OX.Contract.Destroy", Contract_Destroy, 1);
            Register("OX.Contract.GetScript", Contract_GetScript, 1);
            Register("OX.Contract.IsPayable", Contract_IsPayable, 1);
            Register("OX.Contract.GetStorageContext", Contract_GetStorageContext, 1);
            Register("OX.Storage.GetContext", Storage_GetContext, 1);
            Register("OX.Storage.GetReadOnlyContext", Storage_GetReadOnlyContext, 1);
            Register("OX.Storage.Get", Storage_Get, 100);
            Register("OX.Storage.Put", Storage_Put);
            Register("OX.Storage.Delete", Storage_Delete, 100);
            Register("OX.Storage.Find", Storage_Find, 1);
            Register("OX.StorageContext.AsReadOnly", StorageContext_AsReadOnly, 1);
            Register("OX.Enumerator.Create", Enumerator_Create, 1);
            Register("OX.Enumerator.Next", Enumerator_Next, 1);
            Register("OX.Enumerator.Value", Enumerator_Value, 1);
            Register("OX.Enumerator.Concat", Enumerator_Concat, 1);
            Register("OX.Iterator.Create", Iterator_Create, 1);
            Register("OX.Iterator.Key", Iterator_Key, 1);
            Register("OX.Iterator.Keys", Iterator_Keys, 1);
            Register("OX.Iterator.Values", Iterator_Values, 1);
            Register("OX.Iterator.Concat", Iterator_Concat, 1);
            Register("OX.Blockchain.GetSides", Blockchain_GetSides, 1);
            Register("OX.Blockchain.IsInSide", Blockchain_IsInSide, 1);
            Register("OX.Ethereum.EcRecover", Ethereum_EcRecover, 1);
            Register("OX.Ethereum.EcRecoverString", Ethereum_EcRecoverString, 1);
            #region Aliases
            Register("OX.Iterator.Next", Enumerator_Next, 1);
            Register("OX.Iterator.Value", Enumerator_Value, 1);
            #endregion


        }
        private bool Ethereum_EcRecover(ExecutionEngine engine)
        {
            var message = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            var signature = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            var signer = new Nethereum.Signer.EthereumMessageSigner();
            var address = signer.EncodeUTF8AndEcRecover(message.ToHexString(), signature);
            if (address.IsNullOrEmpty()) return false;
            engine.CurrentContext.EvaluationStack.Push(address.ToLower());
            return true;
        }
        private bool Ethereum_EcRecoverString(ExecutionEngine engine)
        {
            var message = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            var signature = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            var signer = new Nethereum.Signer.EthereumMessageSigner();
            var address = signer.EncodeUTF8AndEcRecover(message, signature);
            if (address.IsNullOrEmpty()) return false;
            engine.CurrentContext.EvaluationStack.Push(address.ToLower());
            return true;
        }
        private bool Blockchain_IsInSide(ExecutionEngine engine)
        {
            //var v1 = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            //var v2 = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            //var v3 = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            UInt160 side_script_hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 scope_script_hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            string contractScriptHash = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            SideSateList sideSateList = Snapshot.Sides.TryGet(scope_script_hash);
            bool isInside = false;
            if (sideSateList.IsNotNull() && sideSateList.SideStateList.IsNotNullAndEmpty())
            {
                var sides = sideSateList.SideStateList.Where(m => m.SideScriptHash == side_script_hash && m.SideTransaction.AuthContract.ToString() == contractScriptHash);
                isInside = sides.IsNotNullAndEmpty();
            }
            engine.CurrentContext.EvaluationStack.Push(isInside);
            //engine.CurrentContext.EvaluationStack.Push(true);
            return true;
        }
        private bool Blockchain_GetSides(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            string contractScriptHash = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            SideSateList sideSateList = Snapshot.Sides.TryGet(hash);
            StackItem[] ss = new StackItem[0];
            if (sideSateList.IsNotNull() && sideSateList.SideStateList.IsNotNullAndEmpty())
            {
                var sides = sideSateList.SideStateList.Where(m => m.SideTransaction.AuthContract.ToString() == contractScriptHash);
                if (sides.IsNotNullAndEmpty())
                {
                    ss = sides.Select(p => (StackItem)p.SideScriptHash.ToArray()).ToArray();
                }
            }
            engine.CurrentContext.EvaluationStack.Push(ss);
            return true;
        }
        private bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AccountState account = Snapshot.Accounts.GetOrAdd(hash, () => new AccountState(hash));
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(account));
            return true;
        }

        private bool Blockchain_GetValidators(ExecutionEngine engine)
        {
            ECPoint[] validators = Snapshot.GetValidators();
            engine.CurrentContext.EvaluationStack.Push(validators.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        private bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Snapshot.Assets.TryGet(hash);
            if (asset == null) return false;
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        private bool Header_GetVersion(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.Version);
                return true;
            }
            return false;
        }

        private bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.MerkleRoot.ToArray());
                return true;
            }
            return false;
        }

        private bool Header_GetConsensusData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.ConsensusData);
                return true;
            }
            return false;
        }

        private bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                BlockBase header = _interface.GetInterface<BlockBase>();
                if (header == null) return false;
                engine.CurrentContext.EvaluationStack.Push(header.NextConsensus.ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)tx.Type);
                return true;
            }
            return false;
        }

        private bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Attributes.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetInputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Inputs.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Outputs.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetReferences(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Inputs.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetUnspentCoins(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                TransactionOutput[] outputs = Snapshot.GetUnspent(tx.Hash).ToArray();
                if (outputs.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(outputs.Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool Transaction_GetWitnesses(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                Transaction tx = _interface.GetInterface<Transaction>();
                if (tx == null) return false;
                if (tx.Witnesses.Length > engine.MaxArraySize)
                    return false;
                engine.CurrentContext.EvaluationStack.Push(WitnessWrapper.Create(tx, Snapshot).Select(p => StackItem.FromInterface(p)).ToArray());
                return true;
            }
            return false;
        }

        private bool InvocationTransaction_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                InvocationTransaction tx = _interface.GetInterface<InvocationTransaction>();
                if (tx == null) return false;
                engine.CurrentContext.EvaluationStack.Push(tx.Script);
                return true;
            }
            return false;
        }

        private bool Witness_GetVerificationScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                WitnessWrapper witness = _interface.GetInterface<WitnessWrapper>();
                if (witness == null) return false;
                engine.CurrentContext.EvaluationStack.Push(witness.VerificationScript);
                return true;
            }
            return false;
        }


        private bool Attribute_GetUsage(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)attr.Usage);
                return true;
            }
            return false;
        }

        private bool Attribute_GetData(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionAttribute attr = _interface.GetInterface<TransactionAttribute>();
                if (attr == null) return false;
                engine.CurrentContext.EvaluationStack.Push(attr.Data);
                return true;
            }
            return false;
        }

        private bool Input_GetHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push(input.PrevHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Input_GetIndex(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                CoinReference input = _interface.GetInterface<CoinReference>();
                if (input == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)input.PrevIndex);
                return true;
            }
            return false;
        }

        private bool Output_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.AssetId.ToArray());
                return true;
            }
            return false;
        }

        private bool Output_GetValue(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.Value.GetData());
                return true;
            }
            return false;
        }

        private bool Output_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                TransactionOutput output = _interface.GetInterface<TransactionOutput>();
                if (output == null) return false;
                engine.CurrentContext.EvaluationStack.Push(output.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetScriptHash(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.ScriptHash.ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetVotes(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                if (account == null) return false;
                engine.CurrentContext.EvaluationStack.Push(account.Votes.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
                return true;
            }
            return false;
        }

        private bool Account_GetBalance(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AccountState account = _interface.GetInterface<AccountState>();
                UInt256 asset_id = new UInt256(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
                if (account == null) return false;
                Fixed8 balance = account.Balances.TryGetValue(asset_id, out Fixed8 value) ? value : Fixed8.Zero;
                engine.CurrentContext.EvaluationStack.Push(balance.GetData());
                return true;
            }
            return false;
        }

        private bool Account_IsStandard(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            engine.CurrentContext.EvaluationStack.Push(isStandard);
            return true;
        }

        private bool Asset_Create(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            InvocationTransaction tx = (InvocationTransaction)engine.ScriptContainer;
            AssetType asset_type = (AssetType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (!Enum.IsDefined(typeof(AssetType), asset_type) || asset_type == AssetType.CreditFlag || asset_type == AssetType.DutyFlag || asset_type == AssetType.GoverningToken || asset_type == AssetType.UtilityToken)
                return false;
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 1024)
                return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            Fixed8 amount = new Fixed8((long)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger());
            if (amount == Fixed8.Zero || amount < -Fixed8.Satoshi) return false;
            if (asset_type == AssetType.Invoice && amount != -Fixed8.Satoshi)
                return false;
            byte precision = (byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (precision > 8) return false;
            if (asset_type == AssetType.Share && precision != 0) return false;
            if (amount != -Fixed8.Satoshi && amount.GetData() % (long)Math.Pow(10, 8 - precision) != 0)
                return false;
            ECPoint owner = ECPoint.DecodePoint(engine.CurrentContext.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            if (owner.IsInfinity) return false;
            if (!CheckWitness(engine, owner))
                return false;
            UInt160 admin = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 issuer = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            AssetState asset = Snapshot.Assets.GetOrAdd(tx.Hash, () => new AssetState
            {
                AssetId = tx.Hash,
                AssetType = asset_type,
                Name = name,
                Amount = amount,
                Available = Fixed8.Zero,
                Precision = precision,
                Fee = Fixed8.Zero,
                FeeAddress = new UInt160(),
                Owner = owner,
                Admin = admin,
                Issuer = issuer,
                Expiration = Snapshot.Height + 1 + 2000000,
                IsFrozen = false
            });
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        private bool Asset_Renew(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                byte years = (byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
                asset = Snapshot.Assets.GetAndChange(asset.AssetId);
                if (asset.Expiration < Snapshot.Height + 1)
                    asset.Expiration = Snapshot.Height + 1;
                try
                {
                    asset.Expiration = checked(asset.Expiration + years * 2000000u);
                }
                catch (OverflowException)
                {
                    asset.Expiration = uint.MaxValue;
                }
                engine.CurrentContext.EvaluationStack.Push(asset.Expiration);
                return true;
            }
            return false;
        }

        private bool Asset_GetAssetId(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.AssetId.ToArray());
                return true;
            }
            return false;
        }

        private bool Asset_GetAssetType(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.AssetType);
                return true;
            }
            return false;
        }

        private bool Asset_GetAmount(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Amount.GetData());
                return true;
            }
            return false;
        }

        private bool Asset_GetAvailable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Available.GetData());
                return true;
            }
            return false;
        }

        private bool Asset_GetPrecision(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push((int)asset.Precision);
                return true;
            }
            return false;
        }

        private bool Asset_GetOwner(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Owner.EncodePoint(true));
                return true;
            }
            return false;
        }

        private bool Asset_GetAdmin(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Admin.ToArray());
                return true;
            }
            return false;
        }

        private bool Asset_GetIssuer(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return false;
                engine.CurrentContext.EvaluationStack.Push(asset.Issuer.ToArray());
                return true;
            }
            return false;
        }

        private bool Contract_Create(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.CurrentContext.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                Snapshot.Contracts.Add(hash, contract);
                ContractsCreated.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private bool Contract_Migrate(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) return false;
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.CurrentContext.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            if (engine.CurrentContext.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                Snapshot.Contracts.Add(hash, contract);
                ContractsCreated.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
                if (contract.HasStorage)
                {
                    foreach (var pair in Snapshot.Storages.Find(engine.CurrentContext.ScriptHash).ToArray())
                    {
                        Snapshot.Storages.Add(new StorageKey
                        {
                            ScriptHash = hash,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value,
                            IsConstant = false
                        });
                    }
                }
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return Contract_Destroy(engine);
        }

        private bool Contract_GetScript(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Script);
                return true;
            }
            return false;
        }

        private bool Contract_IsPayable(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (contract == null) return false;
                engine.CurrentContext.EvaluationStack.Push(contract.Payable);
                return true;
            }
            return false;
        }

        private bool Storage_Find(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
                byte[] prefix_key;
                using (MemoryStream ms = new MemoryStream())
                {
                    int index = 0;
                    int remain = prefix.Length;
                    while (remain >= 16)
                    {
                        ms.Write(prefix, index, 16);
                        ms.WriteByte(0);
                        index += 16;
                        remain -= 16;
                    }
                    if (remain > 0)
                        ms.Write(prefix, index, remain);
                    prefix_key = context.ScriptHash.ToArray().Concat(ms.ToArray()).ToArray();
                }
                StorageIterator iterator = new StorageIterator(Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.Take(prefix.Length).SequenceEqual(prefix)).GetEnumerator());
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                Disposables.Add(iterator);
                return true;
            }
            return false;
        }

        private bool Enumerator_Create(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is VMArray array)
            {
                IEnumerator enumerator = new ArrayWrapper(array);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
                return true;
            }
            return false;
        }

        private bool Enumerator_Next(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private bool Enumerator_Value(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private bool Enumerator_Concat(ExecutionEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private bool Iterator_Create(ExecutionEngine engine)
        {
            IIterator iterator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
            {
                case VMArray array:
                    iterator = new ArrayWrapper(array);
                    break;
                case Map map:
                    iterator = new MapWrapper(map);
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
            return true;
        }

        private bool Iterator_Key(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        private bool Iterator_Keys(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        private bool Iterator_Values(ExecutionEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }

        private bool Iterator_Concat(ExecutionEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IIterator first = _interface1.GetInterface<IIterator>();
            IIterator second = _interface2.GetInterface<IIterator>();
            IIterator result = new ConcatenatedIterator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }
    }
}
