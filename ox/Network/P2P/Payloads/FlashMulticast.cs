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
using static Akka.Actor.ProviderSelection;

namespace OX.Network.P2P.Payloads
{
    public class FlashMulticast : FlashMessage
    {
        public UInt256 TalkLine;
        public byte[] Data;
        public override int Size => base.Size + TalkLine.Size + Data.GetVarSize();
        public FlashMulticast() : base(FlashMessageType.FlashMulticast)
        {
            Data = new byte[] { 0x00 };
        }
        public FlashMulticast(KeyPair local, uint minIndex, byte[] shareKey, byte[] plaintext) : this()
        {
            this.Sender = local.PublicKey;
            this.MinIndex = minIndex;
            this.TalkLine = new UInt256(Crypto.Default.Hash256(Crypto.Default.Hash256(shareKey)));
            this.Data = plaintext.Encrypt(shareKey, BitConverter.GetBytes(MinIndex));
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            TalkLine = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(TalkLine);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["tohash"] = TalkLine.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }
        public bool TryDecrypt(KeyPair local, out byte[] plaintext)
        {
            plaintext = this.Data.Decrypt(local, this.Sender, BitConverter.GetBytes(MinIndex));
            return plaintext != default;
        }
        public bool TryDecrypt(IEnumerable<byte> shareKey, out byte[] plaintext)
        {
            plaintext = this.Data.Decrypt(shareKey, BitConverter.GetBytes(MinIndex));
            return plaintext != default;
        }
    }
}
