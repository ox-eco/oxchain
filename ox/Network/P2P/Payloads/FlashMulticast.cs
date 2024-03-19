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
using OX.Wallets;

namespace OX.Network.P2P.Payloads
{
    public class FlashMulticast : FlashState
    {
        public UInt256 ToHash;
        public byte[] Data;
        public override int Size => base.Size + ToHash.Size + Data.GetVarSize();
        public FlashMulticast() : base(FlashStateType.FlashMulticast)
        {
            Data = new byte[] { 0x00 };
        }
        public FlashMulticast(KeyPair local,uint minIndex,byte[] shareKey, byte[] plaintext) : this()
        {
            this.Sender = local.PublicKey;
            this.MinIndex = minIndex;
            this.ToHash = new UInt256(Crypto.Default.Hash256(shareKey));
            this.Data = plaintext.Encrypt(shareKey, BitConverter.GetBytes(MinIndex));
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            ToHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(ToHash);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["tohash"] = ToHash.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
