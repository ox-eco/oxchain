using OX.Cryptography.ECC;
using OX.IO;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.Persistence.LevelDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OX.Persistence
{
    public static class FlashStateHelper
    {
        static int _poolMultiple = 0;
        /// <summary>
        /// 1:black list
        /// 2:white list
        /// </summary>
        static int _listKind = 0; 
        static ContractState _contractState = default;
        static byte[] _intervalFunctionScriptHash = default;
        static UInt160[] _blackList = default;
        static UInt160[] _whiteList = default;
        static readonly Dictionary<UInt160, byte[]> _domains = new Dictionary<UInt160, byte[]>();
        static readonly Dictionary<UInt160, byte[]> _marks = new Dictionary<UInt160, byte[]>();
        static readonly ReaderWriterLockSlim _domainRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public static UInt160[] GetBlackList(this Blockchain blockchain)
        {
            if (blockchain.HeaderHeight % 10 == 0)
            {
                _blackList = GetBlackList();
            }
            return _blackList;
        }
        public static bool InBlackList(this Blockchain blockchain, UInt160 address)
        {
            var list = blockchain.GetBlackList();
            if (list.IsNullOrEmpty()) return false;
            return list.Contains(address);
        }
        public static UInt160[] GetBlackList()
        {
            var kyes = Blockchain.FlashStateContractScriptHash.ToArray().Concat(System.Text.Encoding.UTF8.GetBytes("bkl")).Concat(new byte[] { 0 });
            var kbs = Blockchain.Singleton.Store.GetAll(Prefixes.ST_Storage, kyes.ToArray());
            if (kbs.IsNotNullAndEmpty())
            {
                return kbs.Select(m => new UInt160(m.Value.AsSerializable<StorageItem>().Value)).ToArray();
            }
            return default;
        }

        public static UInt160[] GetWhiteList(this Blockchain blockchain)
        {
            if (blockchain.HeaderHeight % 10 == 0)
            {
                _whiteList = GetWhiteList();
            }
            return _whiteList;
        }
        public static bool InWhiteList(this Blockchain blockchain, UInt160 address)
        {
            var list = blockchain.GetWhiteList();
            if (list.IsNullOrEmpty()) return false;
            return list.Contains(address);
        }
        public static UInt160[] GetWhiteList()
        {
            var kyes = Blockchain.FlashStateContractScriptHash.ToArray().Concat(System.Text.Encoding.UTF8.GetBytes("wtl")).Concat(new byte[] { 0 });
            var kbs = Blockchain.Singleton.Store.GetAll(Prefixes.ST_Storage, kyes.ToArray());
            if (kbs.IsNotNullAndEmpty())
            {
                return kbs.Select(m => new UInt160(m.Value.AsSerializable<StorageItem>().Value)).ToArray();
            }
            return default;
        }


        public static bool GetDomain(this Blockchain blockchain, UInt160 address, out byte[] domain)
        {
            _domainRwLock.EnterReadLock();
            try
            {
                if (!_domains.TryGetValue(address, out domain))
                {
                    domain = GetDomain(address);
                    if (domain != default)
                    {
                        _domains[address] = domain;
                    }
                }
                return domain != default;
            }
            finally
            {
                _domainRwLock.ExitReadLock();
            }
        }
        public static byte[] GetMark(this Blockchain blockchain, UInt160 address)
        {
            _domainRwLock.EnterReadLock();
            try
            {
                if (!_marks.TryGetValue(address, out byte[] result) || blockchain.HeaderHeight % 10 == 0)
                {
                    result = GetMark(address);
                    if (result != default)
                    {
                        _marks[address] = result;
                    }
                }
                return result;
            }
            finally
            {
                _domainRwLock.ExitReadLock();
            }
        }
        public static byte[] GetIntervalFunctionScriptHash(this Blockchain blockchain, out ContractState contractState)
        {
            if (blockchain.HeaderHeight % 100 == 0 || _intervalFunctionScriptHash == default)
            {
                _intervalFunctionScriptHash = GetIntervalFunctionScriptHash();
                if (_intervalFunctionScriptHash != default)
                {
                    _contractState = blockchain.Store.GetContracts().TryGet(new UInt160(_intervalFunctionScriptHash));
                }
                else
                    _contractState = default;
            }
            contractState = _contractState;
            return _intervalFunctionScriptHash;
        }
        public static int GetPoolMutiple(this Blockchain blockchain)
        {
            if (blockchain.HeaderHeight % 100 == 0 || _poolMultiple == 0)
            {
                _poolMultiple = GetPoolMutiple();

            }
            return _poolMultiple;
        }
        public static int GetListKind(this Blockchain blockchain)
        {
            if (blockchain.HeaderHeight % 100 == 0 || _listKind == 0)
            {
                _listKind = GetListKind();

            }
            return _listKind;
        }
        public static byte[] GetDomain(UInt160 address)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashStateContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("dmsr").Concat(new byte[] { 0 }).Concat(address.ToArray()).ToArray(),
            });
            return item.IsNotNull() ? item.Value.Skip(20).ToArray() : default;
        }
        public static byte[] GetMark(UInt160 address)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashStateContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("mrk").Concat(new byte[] { 0 }).Concat(address.ToArray()).ToArray(),
            });
            return item.IsNotNull() ? item.Value.Skip(20).ToArray() : default;
        }
        public static byte[] GetIntervalFunctionScriptHash()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashStateContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 0 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value : default;
        }
        public static int GetPoolMutiple()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashStateContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 1 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value[0] : 0;
        }
        public static int GetListKind()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashStateContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 2 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value[0] : 0;
        }
        public static bool AllowFlashState(this Blockchain blockchain, AccountState accountState, uint referenceLastFlashIndex = 0)
        {
            var txPoolCount = blockchain.MemPool.Count;
            if (txPoolCount > blockchain.MemPool.RebroadcastMultiplierThreshold * blockchain.GetPoolMutiple()) return false;
            return blockchain.StatePool.AllowFlashState(accountState, txPoolCount, referenceLastFlashIndex);
        }
    }
}
