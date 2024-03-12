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
        //private readonly Dictionary<UInt160, AccountFlashState> _accountStates = new Dictionary<UInt160, AccountFlashState>();
        private readonly Dictionary<UInt160, FlashAccount> _flashAccounts = new Dictionary<UInt160, FlashAccount>();
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public FlashStatePool(OXSystem system)
        {
            _system = system;
        }
        public int GetAccountFlashStateInterval(AccountState accountState)
        {
            var balance = accountState.GetBalance(Blockchain.OXS);
            if (balance > Fixed8.Zero)
            {
                if (balance >= Blockchain.FlashMinOXSBalance * 100) return 2;
                if (balance >= Blockchain.FlashMinOXSBalance * 10) return 10;
                if (balance >= Blockchain.FlashMinOXSBalance) return 100;
            }
            return 0;
        }
        public bool TryAppend(AccountState accountState, FlashState flashstate, string remoteNodeKey, Action<FlashAccount> action = default)
        {
            _txRwLock.EnterReadLock();
            try
            {
                var interval = GetAccountFlashStateInterval(accountState);
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
                var ret = flashAccount.TryAppenFlashState(flashstate);
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
        //public bool TryAppend(int accountCapacity, UInt160 address, FlashState flashstate, uint index)
        //{
        //    if (accountCapacity == 0) return false;
        //    _txRwLock.EnterReadLock();
        //    try
        //    {
        //        if (!_accountStates.TryGetValue(address, out AccountFlashState accountFlashState))
        //        {
        //            accountFlashState = new AccountFlashState();
        //            _accountStates[address] = accountFlashState;
        //        }
        //        if (index < accountFlashState.LastFlashIndex + 10) return false;
        //        return accountFlashState.AppendFlashState(accountCapacity, new FlashStateItem(flashstate, index));
        //    }
        //    finally
        //    {
        //        _txRwLock.ExitReadLock();
        //    }
        //}
        //public void TryRemove(UInt160 address)
        //{
        //    if (_accountStates.ContainsKey(address))
        //        _accountStates.Remove(address);
        //}
        //public void Crop(UInt160 address, int accountCapacity)
        //{
        //    _txRwLock.EnterReadLock();
        //    try
        //    {
        //        if (accountCapacity == 0)
        //        {
        //            TryRemove(address);
        //        }
        //        else
        //        {
        //            if (_accountStates.TryGetValue(address, out AccountFlashState accountFlashState))
        //            {
        //                accountFlashState.Crop(accountCapacity);
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        _txRwLock.ExitReadLock();
        //    }
        //}
    }
    //public class AccountFlashState
    //{
    //    public uint LastFlashIndex { get; private set; }
    //    private Dictionary<UInt256, FlashStateItem> States = new Dictionary<UInt256, FlashStateItem>();
    //    internal bool AppendFlashState(int accountCapacity, FlashStateItem flashStateItem)
    //    {
    //        LastFlashIndex = flashStateItem.Index;
    //        States[flashStateItem.FlashState.Hash] = flashStateItem;
    //        Crop(accountCapacity);
    //        return accountCapacity > 0;
    //    }
    //    internal void Crop(int accountCapacity)
    //    {
    //        if (accountCapacity == 0)
    //        {
    //            States.Clear();
    //        }
    //        else if (States.Count > accountCapacity)
    //        {
    //            States = States.Values.OrderByDescending(m => m.Index).Take(accountCapacity).ToDictionary(n => n.FlashState.Hash);
    //        }
    //    }
    //}
    //public class FlashStateItem
    //{
    //    public FlashState FlashState { get; private set; }
    //    public uint Index { get; private set; }
    //    public FlashStateItem(FlashState flashstate, uint index)
    //    {
    //        this.FlashState = flashstate;
    //        this.Index = index;
    //    }
    //}
    public class FlashAccount
    {
        public UInt256 LastHash { get; private set; } = UInt256.Zero;
        public uint LastIndex { get; private set; } = 0;
        public FlashState LastFlashState { get; private set; }
        public List<string> InRemoteKeys = new List<string>();
        public List<string> OutRemoteKeys = new List<string>();
        public bool TryAppenFlashState(FlashState flashState)
        {
            if (flashState.MinIndex <= LastIndex) return false;
            if (flashState.Hash.Equals(LastHash)) return false;
            LastHash = flashState.Hash;
            LastFlashState = flashState;
            LastIndex = flashState.MinIndex;
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
}
