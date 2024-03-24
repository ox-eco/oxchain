using Akka.Util.Internal;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
using OX.Plugins;
using OX.SmartContract;
using OX.VM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading;

namespace OX.Ledger
{
    public class FlashMessagePool
    {
        private readonly OXSystem _system;
        private readonly Dictionary<UInt160, FlashAccount> _flashAccounts = new Dictionary<UInt160, FlashAccount>();
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public FlashMessagePool(OXSystem system)
        {
            _system = system;
        }
        public int GetAccountFlashStateInterval(int txPoolCount, Fixed8 oxsBalance)
        {
            if (oxsBalance < Blockchain.FlashMinOXSBalance) return 0;
            var multiple = (int)(oxsBalance.GetInternalValue() / Blockchain.FlashMinOXSBalance.GetInternalValue());

            long totalOXS = 0;
            int totalFS = 0;
            var fas = this._flashAccounts.Where(m => m.Value.LastIndex > Blockchain.Singleton.HeaderHeight - 10);
            if (fas.IsNotNullAndEmpty())
            {
                totalOXS = fas.Sum(m => m.Value.LastOXSBalance.GetInternalValue());
                totalFS = fas.Count();
            }
            return _getFlashStateInterval(txPoolCount, multiple, totalFS, totalOXS);
        }
        int _getFlashStateInterval(int txPoolCount, int balanceMultiple, int flashStateNumber, long totalOXSBalance)
        {
            //if (balanceMultiple < 1) return 0;
            //if (balanceMultiple >= 1000) return 2;
            //if (balanceMultiple >= 100)
            //{
            //    return flashStateNumber > 5000 ? 10 : 2;
            //}
            //if (balanceMultiple >= 10)
            //{
            //    if (flashStateNumber < 1000) return 2;
            //    else if (flashStateNumber < 5000) return 10;
            //    else return 100;
            //}
            //if (flashStateNumber == 0 || flashStateNumber > 5000) return 0;
            //var k = totalOXSBalance / 10000;
            //if (k > 100_000_000L * 5000) return 0;
            //if (k > 100_000_000L * 2000)
            //    return flashStateNumber < 1000 ? 10 : 100;
            //else
            //    return flashStateNumber < 1000 ? 2 : 10;



            var IntervalFunctionScriptHash = Blockchain.Singleton.GetIntervalFunctionScriptHash(out ContractState contractState);
            if (IntervalFunctionScriptHash == default) return 0;
            var parameters = contractState.ParameterList.Select(p => new ContractParameter(p)).ToArray();
            parameters[0].Value = "getflashstateinterval";
            List<ContractParameter> list = new List<ContractParameter>();
            list.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = txPoolCount });
            list.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = balanceMultiple });
            list.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = flashStateNumber });
            list.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = totalOXSBalance });
            parameters[1].Value = list;
            byte[] scripts = default;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(new UInt160(IntervalFunctionScriptHash), parameters);
                scripts = sb.ToArray();
            }
            var tx = new InvocationTransaction();
            tx.Version = 1;
            tx.Script = scripts;
            if (tx.Attributes == null) tx.Attributes = new TransactionAttribute[0];
            if (tx.Inputs == null) tx.Inputs = new CoinReference[0];
            if (tx.Outputs == null) tx.Outputs = new TransactionOutput[0];
            if (tx.Witnesses == null) tx.Witnesses = new Witness[0];
            ApplicationEngine engine = ApplicationEngine.Run(tx.Script, tx, testMode: true);

            if (engine.State.HasFlag(VMState.FAULT)) return 0;
            return (int)engine.ResultStack.Pop().GetBigInteger();
        }
        internal bool AllowFlashState(AccountState accountState, int txPoolCount, uint referenceLastFlashIndex = 0)
        {
            var balance = accountState.GetBalance(Blockchain.OXS);
            var interval = GetAccountFlashStateInterval(txPoolCount, balance);
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
        public bool TryAppend(AccountState accountState, FlashMessage flashstate, string remoteNodeKey, int txPoolCount, Action<FlashAccount> action = default)
        {
            _txRwLock.EnterReadLock();
            try
            {
                var balance = accountState.GetBalance(Blockchain.OXS);
                var interval = GetAccountFlashStateInterval(txPoolCount, balance);
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
        public FlashMessage LastFlashMessage { get; private set; }
        public List<string> InRemoteKeys = new List<string>();
        public List<string> OutRemoteKeys = new List<string>();
        public bool TryAppenFlashState(FlashMessage flashState, Fixed8 oxsBalance)
        {
            if (flashState.MinIndex <= LastIndex) return false;
            if (flashState.Hash.Equals(LastHash)) return false;
            LastHash = flashState.Hash;
            LastFlashMessage = flashState;
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
     
}
