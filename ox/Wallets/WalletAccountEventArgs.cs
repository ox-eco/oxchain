using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OX.Wallets
{
    public enum WalletAccountChangeType : byte
    {
        Register = 0x01,
        Unregister = 0x02
    }
    public class WalletAccountEventArgs : EventArgs
    {
        public WalletAccountChangeType AccountChangeType;
        public UInt160 Account;
    }
}
