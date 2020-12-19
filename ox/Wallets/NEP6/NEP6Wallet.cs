using OX.IO.Json;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UserWallet = OX.Wallets.SQLite.UserWallet;

namespace OX.Wallets.NEP6
{
    public class NEP6Wallet : Wallet
    {
        public override event EventHandler<WalletTransactionEventArgs> WalletTransaction;
        protected readonly WalletIndexer indexer;
        protected readonly string path;
        protected string password;
        protected string name;
        protected Version version;
        public readonly ScryptParameters Scrypt;
        protected readonly Dictionary<UInt160, NEP6Account> accounts;
        protected Dictionary<string, NEP6Partner> Partners;
        protected Dictionary<string, NEP6Stone> Stones;
        protected readonly JObject extra;
        protected readonly Dictionary<UInt256, Transaction> unconfirmed = new Dictionary<UInt256, Transaction>();

        public override string Name => name;
        public override Version Version => version;
        public override uint WalletHeight => indexer != null ? indexer.IndexHeight : default;
        protected JObject wallet;
        public NEP6Wallet(WalletIndexer indexer, string path, string name = null)
        {
            this.indexer = indexer;
            this.path = path;
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    wallet = JObject.Parse(reader);
                }
                this.name = wallet["name"]?.AsString();
                this.version = Version.Parse(wallet["version"].AsString());
                this.Scrypt = ScryptParameters.FromJson(wallet["scrypt"]);
                this.accounts = ((JArray)wallet["accounts"]).Select(p => NEP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
                var jar = wallet["partners"];
                if (jar != default)
                {
                    this.Partners = ((JArray)jar).Select(p => NEP6Partner.FromJson(p)).ToDictionary(p => p.Address);
                }
                var sts = wallet["stones"];
                if (sts != default)
                {
                    this.Stones = ((JArray)sts).Select(p => NEP6Stone.FromJson(p)).ToDictionary(p => $"{p.Type}#{p.Key}");
                }
                this.extra = wallet["extra"];

                indexer?.RegisterAccounts(accounts.Keys);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this.accounts = new Dictionary<UInt160, NEP6Account>();
                this.Partners = new Dictionary<string, NEP6Partner>();
                this.Stones = new Dictionary<string, NEP6Stone>();
                this.extra = JObject.Null;
            }

            if (indexer != null)
            {
                indexer.WalletTransaction += WalletIndexer_WalletTransaction;
            }
        }
        private  void AddAccount(NEP6Account account, bool is_import)
        {
            lock (accounts)
            {
                //bool ok = false;
                if (accounts.TryGetValue(account.ScriptHash, out NEP6Account account_old))
                {
                    account.Label = account_old.Label;
                    account.IsDefault = account_old.IsDefault;
                    account.Lock = account_old.Lock;
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                    else
                    {
                        NEP6Contract contract_old = (NEP6Contract)account_old.Contract;
                        if (contract_old != null)
                        {
                            NEP6Contract contract = (NEP6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                else
                {
                    indexer?.RegisterAccounts(new[] { account.ScriptHash }, is_import ? 0 : Blockchain.Singleton.Height);
                    //ok = true;

                }
                accounts[account.ScriptHash] = account;
                //if (ok)
                //    this.ChangeAccount(WalletAccountChangeEventType.Register, account.ScriptHash);
            }
        }

        public override void ApplyTransaction(Transaction tx)
        {
            lock (unconfirmed)
            {
                unconfirmed[tx.Hash] = tx;
            }
            WalletTransaction?.Invoke(this, new WalletTransactionEventArgs
            {
                Transaction = tx,
                RelatedAccounts = tx.Witnesses.Select(p => p.ScriptHash).Union(tx.Outputs.Select(p => p.ScriptHash)).Where(p => Contains(p)).ToArray(),
                Height = null,
                Time = DateTime.UtcNow.ToTimestamp()
            });
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(scriptHash);
            }
        }
        public override bool ContainsAndHeld(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.TryGetValue(scriptHash, out NEP6Account account) && !account.WatchOnly;
            }
        }
        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new KeyPair(privateKey);
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, false);
            return account;
        }
        public virtual NEP6Partner AddPartner(string address, string name, string mobile, string remark)
        {
            if (string.IsNullOrWhiteSpace(address))
                return default;
            try
            {
                OX.Wallets.Wallet.ToScriptHash(address);
            }
            catch
            {
                return default;
            }
            if (this.Partners == default)
                this.Partners = new Dictionary<string, NEP6Partner>();
            if (!this.Partners.TryGetValue(address, out NEP6Partner parter))
            {
                parter = new NEP6Partner(address, name, mobile, remark);
                this.Partners[parter.Address] = parter;
            }
            else
            {
                parter.Name = name;
                parter.Remark = remark;
            }
            return parter;
        }
        public virtual NEP6Stone AddStone(string type, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(type))
                return default;
            if (string.IsNullOrWhiteSpace(key))
                return default;

            if (this.Stones == default)
                this.Stones = new Dictionary<string, NEP6Stone>();
            if (!this.Stones.TryGetValue($"{type}#{key}", out NEP6Stone stone))
            {
                stone = new NEP6Stone(key, type, value);
                this.Stones[$"{type}#{key}"] = stone;
            }
            else
            {
                stone.Value = value;
            }
            return stone;
        }
        public virtual bool DeletePartner(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;
            try
            {
                OX.Wallets.Wallet.ToScriptHash(address);
            }
            catch
            {
                return false;
            }
            return Partners.Remove(address);
        }
        public virtual bool DeleteStone(string type, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (string.IsNullOrWhiteSpace(type))
                return false;

            return this.Stones.Remove($"{type}#{key}");
        }
        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            NEP6Contract nep6contract = contract as NEP6Contract;
            if (nep6contract == null)
            {
                nep6contract = new NEP6Contract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList,
                    ParameterNames = contract.ParameterList.Select((p, i) => $"parameter{i}").ToArray(),
                    Deployed = false
                };
            }
            NEP6Account account;
            if (key == null)
                account = new NEP6Account(this, nep6contract.ScriptHash);
            else
                account = new NEP6Account(this, nep6contract.ScriptHash, key, password);
            account.Contract = nep6contract;
            AddAccount(account, false);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            NEP6Account account = new NEP6Account(this, scriptHash);
            AddAccount(account, true);
            return account;
        }

        public virtual KeyPair DecryptKey(string nep2key)
        {
            return new KeyPair(GetPrivateKeyFromNEP2(nep2key, password, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            bool removed;
            lock (accounts)
            {
                removed = accounts.Remove(scriptHash);
            }
            if (removed)
            {
                indexer?.UnregisterAccounts(new[] { scriptHash });
                //this.ChangeAccount(WalletAccountChangeEventType.Unregister, scriptHash);
            }
            return removed;
        }

        public override void Dispose()
        {
            if (indexer != null)
            {
                indexer.WalletTransaction -= WalletIndexer_WalletTransaction;
            }
        }

        public override Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount, UInt160[] from)
        {
            return FindUnspentCoins(FindUnspentCoins(from).ToArray().Where(p => GetAccount(p.Output.ScriptHash).Contract.Script.IsSignatureContract()), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount, from);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out NEP6Account account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values)
                    yield return account;
            }
        }
        public override IEnumerable<WalletAccount> GetHeldAccounts()
        {
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values.Where(m => !m.WatchOnly))
                    yield return account;
            }
        }
        public IEnumerable<NEP6Partner> GetPartners()
        {
            if (Partners == default)
                Partners = new Dictionary<string, NEP6Partner>();
            lock (Partners)
            {
                foreach (NEP6Partner partner in Partners.Values)
                    yield return partner;
            }
        }
        public IEnumerable<NEP6Stone> GetStones(string type)
        {
            if (this.Stones == default)
                Stones = new Dictionary<string, NEP6Stone>();
            lock (Stones)
            {
                foreach (NEP6Stone stone in Stones.Values)
                    if (stone.Type == type)
                        yield return stone;
            }
        }
        public override IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts)
        {
            if (indexer == null)
                return Enumerable.Empty<Coin>();

            if (unconfirmed.Count == 0)
                return indexer.GetCoins(accounts);
            else
                return GetCoinsInternal();
            IEnumerable<Coin> GetCoinsInternal()
            {
                HashSet<CoinReference> inputs, claims;
                Coin[] coins_unconfirmed;
                lock (unconfirmed)
                {
                    inputs = new HashSet<CoinReference>(unconfirmed.Values.SelectMany(p => p.Inputs));
                    claims = new HashSet<CoinReference>(unconfirmed.Values.OfType<ClaimTransaction>().SelectMany(p => p.Claims));
                    coins_unconfirmed = unconfirmed.Values.Select(tx => tx.Outputs.Select((o, i) => new Coin
                    {
                        Reference = new CoinReference
                        {
                            PrevHash = tx.Hash,
                            PrevIndex = (ushort)i
                        },
                        Output = o,
                        State = CoinState.Unconfirmed
                    })).SelectMany(p => p).ToArray();
                }
                foreach (Coin coin in indexer.GetCoins(accounts))
                {
                    if (inputs.Contains(coin.Reference))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            yield return new Coin
                            {
                                Reference = coin.Reference,
                                Output = coin.Output,
                                State = coin.State | CoinState.Spent
                            };
                        continue;
                    }
                    else if (claims.Contains(coin.Reference))
                    {
                        continue;
                    }
                    yield return coin;
                }
                HashSet<UInt160> accounts_set = new HashSet<UInt160>(accounts);
                foreach (Coin coin in coins_unconfirmed)
                {
                    if (accounts_set.Contains(coin.Output.ScriptHash))
                        yield return coin;
                }
            }
        }

        public override IEnumerable<UInt256> GetTransactions()
        {
            if (indexer != null)
            {
                foreach (UInt256 hash in indexer.GetTransactions(accounts.Keys))
                    yield return hash;
                lock (unconfirmed)
                {
                    foreach (UInt256 hash in unconfirmed.Keys)
                        yield return hash;
                }
            }
        }

        public override WalletAccount Import(X509Certificate2 cert)
        {
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D);
            }
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string wif)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromWIF(wif));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string nep2, string passphrase)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromNEP2(nep2, passphrase));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account;
            if (Scrypt.N == 16384 && Scrypt.R == 8 && Scrypt.P == 8)
                account = new NEP6Account(this, contract.ScriptHash, nep2);
            else
                account = new NEP6Account(this, contract.ScriptHash, key, passphrase);
            account.Contract = contract;
            AddAccount(account, true);
            return account;
        }

        internal void Lock()
        {
            password = null;
        }

        public static NEP6Wallet Migrate(WalletIndexer indexer, string path, string db3path, string password)
        {
            using (UserWallet wallet_old = UserWallet.Open(indexer, db3path, password))
            {
                NEP6Wallet wallet_new = new NEP6Wallet(indexer, path, wallet_old.Name);
                using (wallet_new.Unlock(password))
                {
                    foreach (WalletAccount account in wallet_old.GetAccounts())
                    {
                        wallet_new.CreateAccount(account.Contract, account.GetKey());
                    }
                }
                return wallet_new;
            }
        }

        public virtual void Save()
        {
            wallet = new JObject();
            wallet["name"] = name;
            wallet["version"] = version.ToString();
            wallet["scrypt"] = Scrypt.ToJson();
            wallet["accounts"] = new JArray(accounts.Values.Select(p => p.ToJson()));
            if (Partners != default && Partners.Count > 0)
                wallet["partners"] = new JArray(Partners.Values.Select(p => p.ToJson()));
            if (Stones != default && Stones.Count > 0)
                wallet["stones"] = new JArray(Stones.Values.Select(p => p.ToJson()));
            wallet["extra"] = extra;
            File.WriteAllText(path, wallet.ToString());
        }

        public virtual IDisposable Unlock(string password)
        {
            if (!VerifyPassword(password))
                throw new CryptographicException();
            this.password = password;
            return new WalletLocker(this);
        }

        public override bool VerifyPassword(string password)
        {
            lock (accounts)
            {
                NEP6Account account = accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = accounts.Values.FirstOrDefault(p => p.HasKey);
                }
                if (account == null) return true;
                if (account.Decrypted)
                {
                    return account.VerifyPassword(password);
                }
                else
                {
                    try
                    {
                        account.GetKey(password);
                        return true;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }
            }
        }

        private void WalletIndexer_WalletTransaction(object sender, WalletTransactionEventArgs e)
        {
            lock (unconfirmed)
            {
                unconfirmed.Remove(e.Transaction.Hash);
            }
            UInt160[] relatedAccounts;
            lock (accounts)
            {
                relatedAccounts = e.RelatedAccounts.Where(p => accounts.ContainsKey(p)).ToArray();
            }
            if (relatedAccounts.Length > 0)
            {
                WalletTransaction?.Invoke(this, new WalletTransactionEventArgs
                {
                    Transaction = e.Transaction,
                    RelatedAccounts = relatedAccounts,
                    Height = e.Height,
                    Time = e.Time
                });
            }
        }
    }
}
