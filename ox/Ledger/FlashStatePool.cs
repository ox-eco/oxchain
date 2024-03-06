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
        private readonly Dictionary<UInt160, AccountFlashState> _accountStates = new Dictionary<UInt160, AccountFlashState>();
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public FlashStatePool(OXSystem system)
        {
            _system = system;
        }
        public bool TryAppend(int accountCapacity, UInt160 address, FlashState flashstate, uint index)
        {
            if (accountCapacity == 0) return false;
            _txRwLock.EnterReadLock();
            try
            {
                if (!_accountStates.TryGetValue(address, out AccountFlashState accountFlashState))
                {
                    accountFlashState = new AccountFlashState();
                    _accountStates[address] = accountFlashState;
                }
                if (index < accountFlashState.LastFlashIndex + 10) return false;
                return accountFlashState.AppendFlashState(accountCapacity, new FlashStateItem(flashstate, index));
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }
        public void TryRemove(UInt160 address)
        {
            if (_accountStates.ContainsKey(address))
                _accountStates.Remove(address);
        }
        public void Crop(UInt160 address, int accountCapacity)
        {
            _txRwLock.EnterReadLock();
            try
            {
                if (accountCapacity == 0)
                {
                    TryRemove(address);
                }
                else
                {
                    if (_accountStates.TryGetValue(address, out AccountFlashState accountFlashState))
                    {
                        accountFlashState.Crop(accountCapacity);
                    }
                }
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }
    }
    public class AccountFlashState
    {
        public uint LastFlashIndex { get; private set; }
        private Dictionary<UInt256, FlashStateItem> States = new Dictionary<UInt256, FlashStateItem>();
        internal bool AppendFlashState(int accountCapacity, FlashStateItem flashStateItem)
        {
            LastFlashIndex = flashStateItem.Index;
            States[flashStateItem.FlashState.Hash] = flashStateItem;
            Crop(accountCapacity);
            return accountCapacity > 0;
        }
        internal void Crop(int accountCapacity)
        {
            if (accountCapacity == 0)
            {
                States.Clear();
            }
            else if (States.Count > accountCapacity)
            {
                States = States.Values.OrderByDescending(m => m.Index).Take(accountCapacity).ToDictionary(n => n.FlashState.Hash);
            }
        }
    }
    public class FlashStateItem
    {
        public FlashState FlashState { get; private set; }
        public uint Index { get; private set; }
        public FlashStateItem(FlashState flashstate, uint index)
        {
            this.FlashState = flashstate;
            this.Index = index;
        }
    }
}
