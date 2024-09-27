using Microsoft.Extensions.Hosting;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static OX.Helper;
using ECPoint = OX.Cryptography.ECC.ECPoint;
using OX.IO;
using OX.Cryptography;
using Akka.Util.Internal;

namespace OX.Cryptography
{
    public static class MutualLockHelper
    {
        public static UInt256 CreateRandomApproveHash(this KeyPair key, out UInt256 approveCode, out UInt256 approveSource)
        {
            var pk = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(pk);
            }
            return key.CreateRandomApproveHash(pk, out approveCode, out approveSource);
        }
        public static UInt256 CreateRandomApproveSource()
        {
            var pk = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(pk);
            }
            return new UInt256(Crypto.Default.Hash256(Crypto.Default.Hash256(pk)));
        }
        public static UInt256 CreateRandomApproveHash(this KeyPair key, byte[] randomKey, out UInt256 approveCode, out UInt256 approveSource)
        {
            approveSource = new UInt256(Crypto.Default.Hash256(Crypto.Default.Hash256(randomKey)));
            return key.RebuildApproveHash(approveSource, out approveCode);
        }
        public static UInt256 RebuildApproveHash(this KeyPair key, UInt256 approveSource, out UInt256 approveCode)
        {
            var privateKeyHash = new UInt256(Crypto.Default.Hash256(Crypto.Default.Hash256(key.PrivateKey)));
            var approveData = privateKeyHash.ToArray().Concat(approveSource.ToArray()).ToArray();
            approveCode = new UInt256(Crypto.Default.Hash256(Crypto.Default.Hash256(approveData)));
            return approveCode.Hash;
        }
    }
}
