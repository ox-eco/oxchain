using OX.Network.P2P.Payloads;
using System;

namespace OX.Wallets
{
    public enum WalletAccountChangeEventType : byte
    {
        Register = 0x01,
        Unregister = 0x02,
        Changed = 0x03
    }
    public class WalletTransactionEventArgs : EventArgs
    {
        public Transaction Transaction;
        public UInt160[] RelatedAccounts;
        public uint? Height;
        public uint Time;
    }
    public class WalletAccountChangeEventArgs : EventArgs
    {
        public WalletAccountChangeEventType EventType;
        public UInt160 ScriptHash;
    }
}
