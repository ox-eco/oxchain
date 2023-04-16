﻿using OX.Cryptography;
using OX.IO;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.SmartContract;
using OX.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ECPoint = OX.Cryptography.ECC.ECPoint;

namespace OX.Wallets
{
    public abstract class Wallet : IDisposable
    {
        public abstract event EventHandler<WalletTransactionEventArgs> WalletTransaction;
        //public event EventHandler<WalletAccountChangeEventArgs> WalletAccountEvent;
        public abstract string Name { get; }
        public abstract Version Version { get; }
        public abstract uint WalletHeight { get; }

        public abstract void ApplyTransaction(Transaction tx);
        public abstract bool Contains(UInt160 scriptHash);
        public abstract bool ContainsAndHeld(UInt160 scriptHash);
        public abstract WalletAccount CreateAccount(byte[] privateKey);
        public abstract WalletAccount CreateAccount(Contract contract, KeyPair key = null);
        public abstract WalletAccount CreateAccount(UInt160 scriptHash);
        public abstract bool DeleteAccount(UInt160 scriptHash);
        public abstract WalletAccount GetAccount(UInt160 scriptHash);
        public abstract IEnumerable<WalletAccount> GetAccounts();
        public abstract IEnumerable<WalletAccount> GetHeldAccounts();
        public abstract IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts);
        public abstract IEnumerable<UInt256> GetTransactions();

        public WalletAccount CreateAccount()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }
        //public void ChangeAccount(WalletAccountChangeEventType eventType, UInt160 scriptHash)
        //{
        //    WalletAccountEvent?.Invoke(this, new WalletAccountChangeEventArgs() { EventType = eventType, ScriptHash = scriptHash });
        //}

        public WalletAccount CreateAccount(Contract contract, byte[] privateKey)
        {
            if (privateKey == null) return CreateAccount(contract);
            return CreateAccount(contract, new KeyPair(privateKey));
        }

        public virtual void Dispose()
        {
        }

        public IEnumerable<Coin> FindUnspentCoins(params UInt160[] from)
        {
            IEnumerable<UInt160> accounts = from.Length > 0 ? from : GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash);
            return GetCoins(accounts).Where(p => p.State.HasFlag(CoinState.Confirmed) && !p.State.HasFlag(CoinState.Spent) && !p.State.HasFlag(CoinState.Frozen));
        }

        public virtual Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount, params UInt160[] from)
        {
            return FindUnspentCoins(FindUnspentCoins(from), asset_id, amount);
        }

        protected static Coin[] FindUnspentCoins(IEnumerable<Coin> unspents, UInt256 asset_id, Fixed8 amount)
        {
            Coin[] unspents_asset = unspents.Where(p => p.Output.AssetId == asset_id).ToArray();
            Fixed8 sum = unspents_asset.Sum(p => p.Output.Value);
            if (sum < amount) return null;
            if (sum == amount) return unspents_asset;
            Coin[] unspents_ordered = unspents_asset.OrderByDescending(p => p.Output.Value).ToArray();
            int i = 0;
            while (unspents_ordered[i].Output.Value <= amount)
                amount -= unspents_ordered[i++].Output.Value;
            if (amount == Fixed8.Zero)
                return unspents_ordered.Take(i).ToArray();
            else
                return unspents_ordered.Take(i).Concat(new[] { unspents_ordered.Last(p => p.Output.Value >= amount) }).ToArray();
        }

        public WalletAccount GetAccount(ECPoint pubkey)
        {
            return GetAccount(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        public Fixed8 GetAvailable(UInt256 asset_id)
        {
            return FindUnspentCoins().Where(p => p.Output.AssetId.Equals(asset_id)).Sum(p => p.Output.Value);
        }
        public Fixed8 GeAccountAvailable(UInt160 account, UInt256 asset_id)
        {
            return FindUnspentCoins(account).Where(p => p.Output.AssetId.Equals(asset_id)).Sum(p => p.Output.Value);
        }
        public BigDecimal GetAvailable(UIntBase asset_id)
        {
            if (asset_id is UInt160 asset_id_160)
            {
                byte[] script;
                UInt160[] accounts = GetAccounts().Where(p => !p.WatchOnly).Select(p => p.ScriptHash).ToArray();
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitPush(0);
                    foreach (UInt160 account in accounts)
                    {
                        sb.EmitAppCall(asset_id_160, "balanceOf", account);
                        sb.Emit(OpCode.ADD);
                    }
                    sb.EmitAppCall(asset_id_160, "decimals");
                    script = sb.ToArray();
                }
                using (ApplicationEngine engine = ApplicationEngine.Run(script, extraGAS: Fixed8.FromDecimal(0.2m) * accounts.Length))
                {
                    if (engine.State.HasFlag(VMState.FAULT))
                        return new BigDecimal(0, 0);
                    byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
                    BigInteger amount = engine.ResultStack.Pop().GetBigInteger();
                    return new BigDecimal(amount, decimals);
                }
            }
            else
            {
                return new BigDecimal(GetAvailable((UInt256)asset_id).GetData(), 8);
            }
        }

        public Fixed8 GetBalance(UInt256 asset_id)
        {
            return GetCoins(GetAccounts().Select(p => p.ScriptHash)).Where(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(asset_id)).Sum(p => p.Output.Value);
        }

        public virtual UInt160 GetChangeAddress()
        {
            WalletAccount[] accounts = GetAccounts().ToArray();
            WalletAccount account = accounts.FirstOrDefault(p => p.IsDefault);
            if (account == null)
                account = accounts.FirstOrDefault(p => p.Contract?.Script.IsSignatureContract() == true);
            if (account == null)
                account = accounts.FirstOrDefault(p => !p.WatchOnly);
            if (account == null)
                account = accounts.FirstOrDefault();
            return account?.ScriptHash;
        }

        public IEnumerable<Coin> GetCoins()
        {
            return GetCoins(GetAccounts().Select(p => p.ScriptHash));
        }

        public static byte[] GetPrivateKeyFromNEP2(string nep2, string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            if (nep2 == null) throw new ArgumentNullException(nameof(nep2));
            if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));
            byte[] data = nep2.Base58CheckDecode();
            if (data.Length != 39 || data[0] != 0x01 || data[1] != 0x42 || data[2] != 0xe0)
                throw new FormatException();
            byte[] addresshash = new byte[4];
            Buffer.BlockCopy(data, 3, addresshash, 0, 4);
            byte[] derivedkey = SCrypt.DeriveKey(Encoding.UTF8.GetBytes(passphrase), addresshash, N, r, p, 64);
            byte[] derivedhalf1 = derivedkey.Take(32).ToArray();
            byte[] derivedhalf2 = derivedkey.Skip(32).ToArray();
            byte[] encryptedkey = new byte[32];
            Buffer.BlockCopy(data, 7, encryptedkey, 0, 32);
            byte[] prikey = XOR(encryptedkey.AES256Decrypt(derivedhalf2), derivedhalf1);
            Cryptography.ECC.ECPoint pubkey = Cryptography.ECC.ECCurve.Secp256r1.G * prikey;
            UInt160 script_hash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            string address = script_hash.ToAddress();
            if (!Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).SequenceEqual(addresshash))
                throw new FormatException();
            return prikey;
        }

        public static byte[] GetPrivateKeyFromWIF(string wif)
        {
            if (wif == null) throw new ArgumentNullException();
            byte[] data = wif.Base58CheckDecode();
            if (data.Length != 34 || data[0] != 0x80 || data[33] != 0x01)
                throw new FormatException();
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(data, 1, privateKey, 0, privateKey.Length);
            Array.Clear(data, 0, data.Length);
            return privateKey;
        }

        public IEnumerable<Coin> GetUnclaimedCoins()
        {
            IEnumerable<UInt160> accounts = GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash);
            IEnumerable<Coin> coins = GetCoins(accounts);
            coins = coins.Where(p => p.Output.AssetId.Equals(Blockchain.OXS_Token.Hash));
            coins = coins.Where(p => p.State.HasFlag(CoinState.Confirmed) && p.State.HasFlag(CoinState.Spent));
            coins = coins.Where(p => !p.State.HasFlag(CoinState.Claimed) && !p.State.HasFlag(CoinState.Frozen));
            return coins;
        }

        public virtual WalletAccount Import(X509Certificate2 cert)
        {
            byte[] privateKey;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                privateKey = ecdsa.ExportParameters(true).D;
            }
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual WalletAccount Import(string wif)
        {
            byte[] privateKey = GetPrivateKeyFromWIF(wif);
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual WalletAccount Import(string nep2, string passphrase)
        {
            byte[] privateKey = GetPrivateKeyFromNEP2(nep2, passphrase);
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }
        public T MakeSingleTransaction<T>(SingleTransactionWrapper<T> tx) where T : Transaction, new()
        {
            T ct = tx.Get();
            return MakeTransaction<T>(ct, tx.From, tx.From);
        }
        public AskTransaction MakeSingleAskTransaction(SingleAskTransactionWrapper txWrapper, UInt160 bizScriptHash, byte DataType, ISerializable askItem, uint maxindex = 0x00, uint minindex = 0x00, byte edgeVersion = 0x00)
        {
            var data = askItem.ToArray();
            return MakeSingleAskTransaction(txWrapper, bizScriptHash, DataType, data, maxindex, minindex, edgeVersion);
        }
        public AskTransaction MakeSingleAskTransaction(SingleAskTransactionWrapper txWrapper, UInt160 bizScriptHash, byte DataType, byte[] data, uint maxindex = 0x00, uint minindex = 0x00, byte edgeVersion = 0x00)
        {
            if (!Blockchain.Singleton.VerifyBizValidator(bizScriptHash, out Fixed8 balance, out Fixed8 askFee)) return default;
            AskTransaction ct = txWrapper.Get();
            ct.EdgeVersion = edgeVersion;
            ct.DataType = DataType;
            ct.Data = data;
            ct.From = txWrapper.Account.GetKey().PublicKey;
            ct.BizScriptHash = bizScriptHash;
            ct.MaxIndex = maxindex;
            ct.MinIndex = minindex;
            if (askFee > Fixed8.Zero)
            {
                List<TransactionOutput> list = new List<TransactionOutput>();
                if (ct.Outputs.IsNotNullAndEmpty())
                    list.AddRange(ct.Outputs);
                list.Add(new TransactionOutput() { AssetId = Blockchain.OXC, ScriptHash = bizScriptHash, Value = askFee });
                ct.Outputs = list.ToArray();
            }
            ct = MakeTransaction<AskTransaction>(ct, txWrapper.From, txWrapper.From);
            return ct;
        }
        public T MakeTransaction<T>(T tx, UInt160 from = null, UInt160 change_address = null, Fixed8 fee = default(Fixed8)) where T : Transaction
        {
            if (tx.Outputs == null) tx.Outputs = new TransactionOutput[0];
            if (tx.Attributes == null) tx.Attributes = new TransactionAttribute[0];
            fee += tx.SystemFee;
            var pay_total = (typeof(T) == typeof(IssueTransaction) ? new TransactionOutput[0] : tx.Outputs).GroupBy(p => p.AssetId, (k, g) => new
            {
                AssetId = k,
                Value = g.Sum(p => p.Value)
            }).ToDictionary(p => p.AssetId);
            if (fee > Fixed8.Zero)
            {
                if (pay_total.ContainsKey(Blockchain.OXC_Token.Hash))
                {
                    pay_total[Blockchain.OXC_Token.Hash] = new
                    {
                        AssetId = Blockchain.OXC_Token.Hash,
                        Value = pay_total[Blockchain.OXC_Token.Hash].Value + fee
                    };
                }
                else
                {
                    pay_total.Add(Blockchain.OXC_Token.Hash, new
                    {
                        AssetId = Blockchain.OXC_Token.Hash,
                        Value = fee
                    });
                }
            }
            var pay_coins = pay_total.Select(p => new
            {
                AssetId = p.Key,
                Unspents = from == null ? FindUnspentCoins(p.Key, p.Value.Value) : FindUnspentCoins(p.Key, p.Value.Value, from)
            }).ToDictionary(p => p.AssetId);
            if (pay_coins.Any(p => p.Value.Unspents == null)) return null;
            var input_sum = pay_coins.Values.ToDictionary(p => p.AssetId, p => new
            {
                p.AssetId,
                Value = p.Unspents.Sum(q => q.Output.Value)
            });
            if (change_address == null) change_address = GetChangeAddress();
            List<TransactionOutput> outputs_new = new List<TransactionOutput>(tx.Outputs);
            foreach (UInt256 asset_id in input_sum.Keys)
            {
                if (input_sum[asset_id].Value > pay_total[asset_id].Value)
                {
                    outputs_new.Add(new TransactionOutput
                    {
                        AssetId = asset_id,
                        Value = input_sum[asset_id].Value - pay_total[asset_id].Value,
                        ScriptHash = change_address
                    });
                }
            }
            tx.Inputs = pay_coins.Values.SelectMany(p => p.Unspents).Select(p => p.Reference).ToArray();
            tx.Outputs = outputs_new.ToArray();
            return tx;
        }
        public Transaction MakeTransaction(List<TransactionAttribute> attributes, IEnumerable<TransferOutput> outputs, UInt160 from = null, UInt160 change_address = null, Fixed8 fee = default(Fixed8))
        {
            Random rand = new Random();
            var cOutputs = outputs.Where(p => !p.IsGlobalAsset).GroupBy(p => new
            {
                AssetId = (UInt160)p.AssetId,
                Account = p.ScriptHash
            }, (k, g) => new
            {
                k.AssetId,
                Value = g.Aggregate(BigInteger.Zero, (x, y) => x + y.Value.Value),
                k.Account
            }).ToArray();
            Transaction tx;
            if (attributes == null) attributes = new List<TransactionAttribute>();

            // Generate nonce
            var nonce = new byte[8];
            rand.NextBytes(nonce);
            attributes.Add(new TransactionAttribute()
            {
                Usage = TransactionAttributeUsage.Remark,
                Data = nonce
            });

            if (cOutputs.Length == 0)
            {
                tx = new ContractTransaction();
            }
            else
            {
                UInt160[] accounts = from == null ? GetAccounts().Where(p => !p.Lock && !p.WatchOnly).Select(p => p.ScriptHash).ToArray() : new[] { from };
                HashSet<UInt160> sAttributes = new HashSet<UInt160>();
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    foreach (var output in cOutputs)
                    {
                        var balances = new List<(UInt160 Account, BigInteger Value)>();
                        foreach (UInt160 account in accounts)
                        {
                            byte[] script;
                            using (ScriptBuilder sb2 = new ScriptBuilder())
                            {
                                sb2.EmitAppCall(output.AssetId, "balanceOf", account);
                                script = sb2.ToArray();
                            }
                            using (ApplicationEngine engine = ApplicationEngine.Run(script))
                            {
                                if (engine.State.HasFlag(VMState.FAULT)) return null;
                                var result = engine.ResultStack.Pop().GetBigInteger();
                                if (result == 0) continue;
                                balances.Add((account, result));
                            }
                        }
                        BigInteger sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                        if (sum < output.Value) return null;
                        if (sum != output.Value)
                        {
                            balances = balances.OrderByDescending(p => p.Value).ToList();
                            BigInteger amount = output.Value;
                            int i = 0;
                            while (balances[i].Value <= amount)
                                amount -= balances[i++].Value;
                            if (amount == BigInteger.Zero)
                                balances = balances.Take(i).ToList();
                            else
                                balances = balances.Take(i).Concat(new[] { balances.Last(p => p.Value >= amount) }).ToList();
                            sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                        }
                        sAttributes.UnionWith(balances.Select(p => p.Account));
                        for (int i = 0; i < balances.Count; i++)
                        {
                            BigInteger value = balances[i].Value;
                            if (i == 0)
                            {
                                BigInteger change = sum - output.Value;
                                if (change > 0) value -= change;
                            }
                            sb.EmitAppCall(output.AssetId, "transfer", balances[i].Account, output.Account, value);
                            sb.Emit(OpCode.THROWIFNOT);
                        }
                    }
                    sb.Emit(OpCode.RET);
                    tx = new InvocationTransaction
                    {
                        Version = 1,
                        Script = sb.ToArray()
                    };
                }
                attributes.AddRange(sAttributes.Select(p => new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Script,
                    Data = p.ToArray()
                }));
            }
            tx.Attributes = attributes.ToArray();
            tx.Inputs = new CoinReference[0];
            tx.Outputs = outputs.Where(p => p.IsGlobalAsset).Select(p => p.ToTxOutput()).ToArray();
            tx.Witnesses = new Witness[0];
            if (tx is InvocationTransaction itx)
            {
                using (ApplicationEngine engine = ApplicationEngine.Run(itx.Script, itx))
                {
                    if (engine.State.HasFlag(VMState.FAULT)) return null;
                    tx = new InvocationTransaction
                    {
                        Version = itx.Version,
                        Script = itx.Script,
                        Gas = InvocationTransaction.GetGas(engine.GasConsumed),
                        Attributes = itx.Attributes,
                        Inputs = itx.Inputs,
                        Outputs = itx.Outputs
                    };
                }
            }
            tx = MakeTransaction(tx, from, change_address, fee);
            return tx;
        }
        public bool Sign(ContractParametersContext context)
        {
            bool fSuccess = false;
            foreach (UInt160 scriptHash in context.ScriptHashes)
            {
                WalletAccount account = GetAccount(scriptHash);
                if (account?.HasKey != true) continue;
                KeyPair key = account.GetKey();
                byte[] signature = context.Verifiable.Sign(key);
                fSuccess |= context.AddSignature(account.Contract, key.PublicKey, signature);
            }
            return fSuccess;
        }

        public static string ToAddress(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = ProtocolSettings.Default.AddressVersion;
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        public static UInt160 ToScriptHash(string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            if (data[0] != ProtocolSettings.Default.AddressVersion)
                throw new FormatException();
            return new UInt160(data.Skip(1).ToArray());
        }
        public abstract bool VerifyPassword(string password);

        protected static byte[] XOR(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) throw new ArgumentException();
            return x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray();
        }
    }
}
