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
    public static class FlashMessageHelper
    {
        static uint _flashMessageSizeMultiple_refreshIndex = 0;
        static int _flashMessageSizeMultiple = 0;
        static uint _poolMultiple_refreshIndex = 0;
        static int _poolMultiple = 0;
        static uint _listKind_refreshIndex = 0;
        /// <summary>
        /// 1:black list
        /// 2:white list
        /// </summary>
        static int _listKind = 0;
        static ContractState _contractState = default;
        static uint _intervalFunctionScriptHash_refreshIndex = 0;
        static byte[] _intervalFunctionScriptHash = default;
        static uint _blackList_refreshIndex = 0;
        static UInt160[] _blackList = default;
        static uint _whiteList_refreshIndex = 0;
        static UInt160[] _whiteList = default;
        static readonly Dictionary<UInt160, byte[]> _domains = new Dictionary<UInt160, byte[]>();
        static readonly Dictionary<string, UInt160> _AddressBydomains = new Dictionary<string, UInt160>();
        static readonly Dictionary<UInt160, byte[]> _marks = new Dictionary<UInt160, byte[]>();
        static readonly ReaderWriterLockSlim _domainRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        static readonly ReaderWriterLockSlim _addressBydomainRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        static readonly ReaderWriterLockSlim _markRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public static UInt160[] GetBlackList(this Blockchain blockchain)
        {
            if (_blackList_refreshIndex == 0 || blockchain.HeaderHeight > _blackList_refreshIndex + 10)
            {
                _blackList = GetBlackList();
                _blackList_refreshIndex = blockchain.HeaderHeight;
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
            var kyes = Blockchain.FlashMessageContractScriptHash.ToArray().Concat(System.Text.Encoding.UTF8.GetBytes("bkl")).Concat(new byte[] { 0 });
            var kbs = Blockchain.Singleton.Store.GetAll(Prefixes.ST_Storage, kyes.ToArray());
            if (kbs.IsNotNullAndEmpty())
            {
                return kbs.Select(m => new UInt160(m.Value.AsSerializable<StorageItem>().Value)).ToArray();
            }
            return default;
        }

        public static UInt160[] GetWhiteList(this Blockchain blockchain)
        {
            if (_whiteList_refreshIndex == 0 || blockchain.HeaderHeight > _whiteList_refreshIndex + 10)
            {
                _whiteList = GetWhiteList();
                _whiteList_refreshIndex = blockchain.HeaderHeight;
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
            var kyes = Blockchain.FlashMessageContractScriptHash.ToArray().Concat(System.Text.Encoding.UTF8.GetBytes("wtl")).Concat(new byte[] { 0 });
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
        public static bool GetAddressByDomain(this Blockchain blockchain, string domain, out UInt160 address)
        {
            _addressBydomainRwLock.EnterReadLock();
            try
            {
                if (!_AddressBydomains.TryGetValue(domain, out address))
                {
                    address = GetAddressByDomain(domain);
                    if (domain != default)
                    {
                        _AddressBydomains[domain] = address;
                    }
                }
                return domain != default;
            }
            finally
            {
                _addressBydomainRwLock.ExitReadLock();
            }
        }
        public static byte[] GetMark(this Blockchain blockchain, UInt160 address)
        {
            _markRwLock.EnterReadLock();
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
                _markRwLock.ExitReadLock();
            }
        }
        public static byte[] GetIntervalFunctionScriptHash(this Blockchain blockchain, out ContractState contractState)
        {
            if (_intervalFunctionScriptHash_refreshIndex == 0 || blockchain.HeaderHeight > _intervalFunctionScriptHash_refreshIndex + 100 || _intervalFunctionScriptHash == default)
            {
                _intervalFunctionScriptHash = GetIntervalFunctionScriptHash();
                _intervalFunctionScriptHash_refreshIndex = blockchain.HeaderHeight;
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
            if (_poolMultiple_refreshIndex == 0 || blockchain.HeaderHeight > _poolMultiple_refreshIndex + 100 || _poolMultiple == 0)
            {
                _poolMultiple = GetPoolMutiple();
                _poolMultiple_refreshIndex = blockchain.HeaderHeight;

            }
            return _poolMultiple;
        }
        public static int GetListKind(this Blockchain blockchain)
        {
            if (_listKind_refreshIndex == 0 || blockchain.HeaderHeight > _listKind_refreshIndex + 100 || _listKind == 0)
            {
                _listKind = GetListKind();
                _listKind_refreshIndex = blockchain.HeaderHeight;
            }
            return _listKind;
        }
        public static int GetFlashMessageSizeMutiple(this Blockchain blockchain)
        {
            if (_flashMessageSizeMultiple_refreshIndex == 0 || blockchain.HeaderHeight > _flashMessageSizeMultiple_refreshIndex + 100 || _flashMessageSizeMultiple == 0)
            {
                _flashMessageSizeMultiple = GetFlashMessageSizeMutiple();
                _flashMessageSizeMultiple_refreshIndex = blockchain.HeaderHeight;

            }
            return _flashMessageSizeMultiple;
        }
        public static byte[] GetDomain(UInt160 address)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("dmsr").Concat(new byte[] { 0 }).Concat(address.ToArray()).ToArray(),
            });
            return item.IsNotNull() ? item.Value.Skip(20).ToArray() : default;
        }
        public static UInt160 GetAddressByDomain(string domain)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("dms").Concat(new byte[] { 0 }).Concat(System.Text.Encoding.UTF8.GetBytes(domain)).ToArray(),
            });

            return item.IsNotNull() ? new UInt160(item.Value.Take(20).ToArray()) : default;
        }
        public static byte[] GetMark(UInt160 address)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("mrk").Concat(new byte[] { 0 }).Concat(address.ToArray()).ToArray(),
            });
            return item.IsNotNull() ? item.Value.Skip(20).ToArray() : default;
        }
        public static byte[] GetIntervalFunctionScriptHash()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 0 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value : default;
        }
        public static int GetFlashMessageSizeMutiple()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 3 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value[0] : 0;
        }
        public static int GetPoolMutiple()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 1 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value[0] : 0;
        }
        public static int GetListKind()
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = Blockchain.FlashMessageContractScriptHash,
                Key = System.Text.Encoding.UTF8.GetBytes("itv").Concat(new byte[] { 0 }).Concat(new byte[] { 2 }).ToArray(),
            });
            return item.IsNotNull() ? item.Value[0] : 0;
        }
        public static bool AllowFlashMessage(this Blockchain blockchain, AccountState accountState, out uint expireIndex)
        {
            expireIndex = 0;
            var txPoolCount = blockchain.MemPool.Count;
            if (txPoolCount > blockchain.MemPool.RebroadcastMultiplierThreshold * blockchain.GetPoolMutiple()) return false;
            return blockchain.StatePool.AllowFlashMessage(accountState, txPoolCount, out expireIndex);
        }
    }
}
