using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.Cryptography.ECC;

namespace OX.Wallets
{
    public class AccountPack
    {
        public UInt160 Address { get; set; }
        public KeyPair Key { get; set; }
        public ECPoint PublicKey { get; set; }
    }
}
