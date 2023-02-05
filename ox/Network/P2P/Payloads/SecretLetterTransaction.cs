using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.SmartContract;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class SecretLetterTransaction : Transaction
    {
        public ECPoint From;
        public UInt160 To;
        public byte Flag;
        public byte[] Data;

        public override int Size => base.Size + From.Size + To.Size + sizeof(byte) + Data.GetVarSize();

        public SecretLetterTransaction()
            : base(TransactionType.SecretLetterTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Flag = 0;
            this.Data = new byte[0];
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return Contract.CreateSignatureRedeemScript(this.From).ToScriptHash();
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            From = reader.ReadSerializable<ECPoint>();
            To = reader.ReadSerializable<UInt160>();
            Flag = reader.ReadByte();
            Data = reader.ReadVarBytes();
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Flag);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["from"] = this.From.ToString();
            json["to"] = this.To.ToAddress();
            json["flag"] = this.Flag.ToString();
            json["data"] = this.Data.ToHexString();
            return json;
        }
    }
}
