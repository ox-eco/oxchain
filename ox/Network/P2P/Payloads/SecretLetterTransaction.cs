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
    public class SecretLetterBody : ISerializable
    {
        public UInt256 LetterLine;
        public byte[] KeySuffix;
        public byte[] Encryptedcontent;
        public virtual int Size => LetterLine.Size + KeySuffix.GetVarSize() + Encryptedcontent.GetVarSize();
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(LetterLine);
            writer.WriteVarBytes(KeySuffix);
            writer.WriteVarBytes(Encryptedcontent);
        }
        public void Deserialize(BinaryReader reader)
        {
            LetterLine = reader.ReadSerializable<UInt256>();
            KeySuffix = reader.ReadVarBytes();
            Encryptedcontent = reader.ReadVarBytes();
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
        public SecretLetterTransaction(KeyPair local, ECPoint remote, byte[] KeySuffix, string content)
            : this()
        {
            var letterLine = ECDiffieHellmanHelper.ECDHDeriveKeyHash(local, remote);
            var contentData = System.Text.Encoding.UTF8.GetBytes(content);
            SecretLetterBody body = new SecretLetterBody { LetterLine = letterLine, KeySuffix = KeySuffix, Encryptedcontent = contentData.Encrypt(local, remote, KeySuffix) };
            this.From = local.PublicKey;
            this.ToHash = Contract.CreateSignatureRedeemScript(remote).ToScriptHash().Hash;
            this.Flag = 1;
            this.Data = body.ToArray();
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
        public bool TryGetBody(out SecretLetterBody body)
        {
            body = null;
            try
            {
                body = this.Data.AsSerializable<SecretLetterBody>();
                return body.IsNotNull();
            }
            catch
            {
                return false;
            }
        }
        public bool TryDecrypt(KeyPair local, out SecretLetterBody body, out byte[] plaintext)
        {
            var ret = TryGetBody(out body);
            plaintext = body.Encryptedcontent.Decrypt(local, this.From, body.KeySuffix);
            return ret;
        }
        public bool TryDecrypt(KeyPair local, ECPoint remote, out SecretLetterBody body, out byte[] plaintext)
        {
            var ret = TryGetBody(out body);
            plaintext = body.Encryptedcontent.Decrypt(local, remote, body.KeySuffix);
            return ret;
        }
    }
}
