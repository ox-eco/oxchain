using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.SmartContract;

namespace OX.Network.P2P.Payloads
{
    public abstract class EdgeTransaction : BizTransaction
    {
        public ECPoint From;
        public byte EdgeVersion;
        public byte DataType;
        public byte[] Data;
        public override int Size => base.Size + sizeof(byte) + sizeof(byte) + From.Size + Data.GetVarSize();
        public EdgeTransaction(TransactionType type)
            : base(type)
        {
        }
        protected abstract void DeserializeEdgeData(BinaryReader reader);
        protected abstract void SerializeEdgeData(BinaryWriter writer);
        protected override void DeserializeBizData(BinaryReader reader)
        {
            From = reader.ReadSerializable<ECPoint>();
            EdgeVersion = reader.ReadByte();
            DataType = reader.ReadByte();
            Data = reader.ReadVarBytes();
            DeserializeEdgeData(reader);
        }
        protected override void SerializeBizData(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(EdgeVersion);
            writer.Write(DataType);
            writer.WriteVarBytes(Data);
            SerializeEdgeData(writer);
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
        public bool GetDataModel<T>(UInt160[] bizScriptHashs, byte dataType, out T model) where T : ISerializable, new()
        {
            if (bizScriptHashs.IsNullOrEmpty() || !bizScriptHashs.Contains(this.BizScriptHash) || this.DataType != dataType)
            {
                model = default;
                return false;
            }
            try
            {
                model = this.Data.AsSerializable<T>();
                return true;
            }
            catch
            {
                model = default;
                return false;
            }
        }
    }
}
