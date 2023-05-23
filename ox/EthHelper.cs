using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using OX.IO;
using OX.Network.P2P.Payloads;
using System.Runtime.CompilerServices;
using OX.Ledger;

namespace OX
{

    public static class EthHelper
    {
        public static UInt160 BuildMapAddress(this string ethAddress, uint lockindex = 0)
        {
            return new EthereumMapTransaction { EthereumAddress = ethAddress, LockExpirationIndex = lockindex }.GetContract().ScriptHash;
        }
        
        public static uint BuildAddressId(this UInt160 oxAddress)
        {
            return BitConverter.ToUInt32(oxAddress.ToArray());
        }
        public static uint BuildAddressId(this string ethAddress)
        {
            return BitConverter.ToUInt32(ethAddress.BuildMapAddress().ToArray());
        }
       
    }
}
