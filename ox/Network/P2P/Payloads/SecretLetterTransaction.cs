using OX.Cryptography.ECC;
using OX.Cryptography;
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
    public class SecretLetterData : ISerializable
    {
        public UInt256[] ReplyTxIds;
        public byte[] Content;
        public virtual int Size => ReplyTxIds.GetVarSize() + Content.GetVarSize();
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ReplyTxIds);
            writer.WriteVarBytes(Content);
        }
        public void Deserialize(BinaryReader reader)
        {
            ReplyTxIds = reader.ReadSerializableArray<UInt256>();
            Content = reader.ReadVarBytes();
        }
        public string GetContentString()
        {
            return System.Text.Encoding.UTF8.GetString(this.Content);
        }
    }
    public class SecretLetterTransaction : Transaction
    {
        public ECPoint From;
        public UInt256 ToHash;
        public byte Flag;
        public byte[] Data;

        public override int Size => base.Size + From.Size + ToHash.Size + sizeof(byte) + Data.GetVarSize();

        public SecretLetterTransaction()
            : base(TransactionType.SecretLetterTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Flag = 0;
            this.Data = new byte[0];
        }
        public SecretLetterTransaction(KeyPair local, ECPoint remote, string content, UInt256[] replyTxIds = default)
            : this()
        {
            var contentData = System.Text.Encoding.UTF8.GetBytes(content);
            SecretLetterData sld = new SecretLetterData { Content = System.Text.Encoding.UTF8.GetBytes(content), ReplyTxIds = replyTxIds ?? [UInt256.Zero] };
            this.From = local.PublicKey;
            this.ToHash = Contract.CreateSignatureRedeemScript(remote).ToScriptHash().Hash;
            this.Flag = 1;
            this.Data = sld.ToArray().Encrypt(local, remote);
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
            ToHash = reader.ReadSerializable<UInt256>();
            Flag = reader.ReadByte();
            Data = reader.ReadVarBytes();
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(ToHash);
            writer.Write(Flag);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["from"] = this.From.ToString();
            json["tohash"] = this.ToHash.ToString();
            json["flag"] = this.Flag.ToString();
            json["data"] = this.Data.ToHexString();
            return json;
        }
        public bool TryDecrypt(KeyPair local, out SecretLetterData secretLetterData)
        {
            secretLetterData = new EncryptData(this.Data).Decrypt<SecretLetterData>(local, this.From);
            return secretLetterData.IsNotNull();
        }
        public bool TryDecrypt(KeyPair local, ECPoint remote, out SecretLetterData secretLetterData)
        {
            secretLetterData = new EncryptData(this.Data).Decrypt<SecretLetterData>(local, remote);
            return secretLetterData.IsNotNull();
        }
    }
}
