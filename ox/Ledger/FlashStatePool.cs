using Akka.Util.Internal;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading;

namespace OX.Ledger
{
    public class FlashStatePool
    {
        private readonly OXSystem _system;
        private readonly Dictionary<UInt160, FlashAccount> _flashAccounts = new Dictionary<UInt160, FlashAccount>();
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public FlashStatePool(OXSystem system)
        {
            _system = system;
        }
        public int GetAccountFlashStateInterval(Fixed8 oxsBalance)
        {
            if (oxsBalance < Blockchain.FlashMinOXSBalance) return 0;
            if (oxsBalance >= Blockchain.FlashMinOXSBalance * 1000) return 2;

            Fixed8 totalOXS = Fixed8.Zero;
            int totalFS = 0;
            var fas = this._flashAccounts.Where(m => m.Value.LastIndex > Blockchain.Singleton.HeaderHeight - 10);
            if (fas.IsNotNullAndEmpty())
            {
                totalOXS = fas.Sum(m => m.Value.LastOXSBalance);
                totalFS = fas.Count();
            }
            if (oxsBalance >= Blockchain.FlashMinOXSBalance * 100)
            {
                return totalFS > 5000 ? 10 : 2;
            }
            if (oxsBalance >= Blockchain.FlashMinOXSBalance * 10)
            {
                if (totalFS < 1000) return 2;
                else if (totalFS < 5000) return 10;
                else return 100;
            }
            if (oxsBalance >= Blockchain.FlashMinOXSBalance)
            {
                if (totalFS == 0 || totalFS > 5000) return 0;

                var k = totalOXS / 10000;
                if (k > Fixed8.OXT * 5) return 0;
                if (k > Fixed8.OXT * 2)
                    return totalFS < 1000 ? 10 : 100;
                else
                    return totalFS < 1000 ? 2 : 10;

            }
            return 0;
        }
        internal bool AllowFlashState(AccountState accountState, uint referenceLastFlashIndex = 0)
        {
            var balance = accountState.GetBalance(Blockchain.OXS);
            var interval = GetAccountFlashStateInterval(balance);
            if (interval == 0) return false;
            if (_flashAccounts.TryGetValue(accountState.ScriptHash, out FlashAccount flashAccount))
            {
                return Blockchain.Singleton.Height >= flashAccount.LastIndex + interval;
            }
            else
            {
                return Blockchain.Singleton.Height >= referenceLastFlashIndex + interval;
            }
        }
        public bool TryAppend(AccountState accountState, FlashState flashstate, string remoteNodeKey, Action<FlashAccount> action = default)
        {
            _txRwLock.EnterReadLock();
            try
            {
                var balance = accountState.GetBalance(Blockchain.OXS);
                var interval = GetAccountFlashStateInterval(balance);
                if (interval == 0) return false;
                if (!_flashAccounts.TryGetValue(accountState.ScriptHash, out FlashAccount flashAccount))
                {
                    flashAccount = new FlashAccount();
                    _flashAccounts[accountState.ScriptHash] = flashAccount;
                }
                if (flashstate.Hash.Equals(flashAccount.LastHash))
                {
                    if (!flashAccount.InRemoteKeys.Contains(remoteNodeKey)) flashAccount.InRemoteKeys.Add(remoteNodeKey);
                }
                else
                {
                    if (flashstate.MinIndex < flashAccount.LastIndex + interval) return false;
                }
                var ret = flashAccount.TryAppenFlashState(flashstate, balance);
                if (action != default)
                {
                    action(flashAccount);
                }
                return ret;
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }
        public bool TryRemoveAccount(UInt160 address)
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _flashAccounts.Remove(address);
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

    }

    public class FlashAccount
    {
        public UInt256 LastHash { get; private set; } = UInt256.Zero;
        public uint LastIndex { get; private set; } = 0;
        public Fixed8 LastOXSBalance { get; private set; } = Fixed8.Zero;
        public FlashState LastFlashState { get; private set; }
        public List<string> InRemoteKeys = new List<string>();
        public List<string> OutRemoteKeys = new List<string>();
        public bool TryAppenFlashState(FlashState flashState, Fixed8 oxsBalance)
        {
            if (flashState.MinIndex <= LastIndex) return false;
            if (flashState.Hash.Equals(LastHash)) return false;
            LastHash = flashState.Hash;
            LastFlashState = flashState;
            LastIndex = flashState.MinIndex;
            LastOXSBalance = oxsBalance;
            InRemoteKeys.Clear();
            OutRemoteKeys.Clear();
            return true;
        }
        public bool VerifyRelay(UInt256 flashStateHash, string RemoteNodeKey)
        {
            if (!flashStateHash.Equals(LastHash)) return false;
            if (InRemoteKeys.Contains(RemoteNodeKey)) return false;
            if (OutRemoteKeys.Contains(RemoteNodeKey)) return false;
            return true;
        }
    }
    public static class FlashStateHelper
    {
        public static bool AllowFlashState(this Blockchain blockchain, AccountState accountState, uint referenceLastFlashIndex = 0)
        {
            if (blockchain.MemPool.Count > blockchain.MemPool.RebroadcastMultiplierThreshold) return false;
            return blockchain.StatePool.AllowFlashState(accountState, referenceLastFlashIndex);
        }
    }
}
