using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.Wallets;
using OX.Cryptography.ECC;

namespace OX.Cryptography.AES
{
    public static class ECDHHelper
    {
        public static ECPoint DiffieHellman(this KeyPair key, ECPoint pubkey)
        {
            return pubkey * key.PrivateKey;
        }
    }
}
