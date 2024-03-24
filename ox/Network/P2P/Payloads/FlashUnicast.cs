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
    public class FlashUnicast : FlashMessage
    {
        public UInt256 TalkLine;
        public UInt256 RecipientHash;
        public byte[] Data;
        public override int Size => base.Size + TalkLine.Size + RecipientHash.Size + Data.GetVarSize();
        public FlashUnicast() : base(FlashMessageType.FlashUnicast)
        {
            Data = new byte[] { 0x00 };
        }
        public FlashUnicast(KeyPair local, ECPoint remote, uint minIndex, byte[] plaintext) : this()
        {
            this.Sender = local.PublicKey;
            this.MinIndex = minIndex;
            this.TalkLine = ECDiffieHellmanHelper.ECDHDeriveKeyHash(local, remote);
            this.RecipientHash = Contract.CreateSignatureRedeemScript(remote).ToScriptHash().Hash;
            this.Data = plaintext.Encrypt(local, remote, BitConverter.GetBytes(MinIndex));
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            TalkLine = reader.ReadSerializable<UInt256>();
            RecipientHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(TalkLine);
            writer.Write(RecipientHash);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["tohash"] = RecipientHash.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }
        public bool TryDecrypt(KeyPair local, out byte[] plaintext)
        {
            plaintext = this.Data.Decrypt(local, this.Sender, BitConverter.GetBytes(MinIndex));
            return plaintext != default;
        }
        public bool TryDecrypt(KeyPair local, ECPoint remote,  out byte[] plaintext)
        {
            plaintext = this.Data.Decrypt(local, remote, BitConverter.GetBytes(MinIndex));
            return plaintext != default;
        }
    }
}
