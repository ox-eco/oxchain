//using Org.BouncyCastle.Math.EC;
using OX.Cryptography;
using OX.IO;
using OX.IO.Caching;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using OX.SmartContract;
using OX.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OX.Cryptography.ECC;
using Org.BouncyCastle.Security.Certificates;
using Nethereum.Signer;
using Nethereum.Signer.Crypto;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Runtime.CompilerServices;

namespace OX.Network.P2P.Payloads
{
    public class FlashState : FlashMessage
    {
        public byte[] Data;
        public override int Size => base.Size + Data.GetVarSize();
        public FlashState() : base(FlashMessageType.FlashState)
        {
            Data = new byte[] { 0x00 };
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
