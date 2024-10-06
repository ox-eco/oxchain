using OX.IO;
using System.Collections.Generic;
using System.IO;
using OX.Persistence;

namespace OX.Network.P2P.Payloads
{
    public abstract class BizTransaction : Transaction
    {
        public const int FreeDataSize = 64;
        public UInt160 BizScriptHash;
        public byte Flag { get; set; }

        public override int Size => base.Size + BizScriptHash.Size + sizeof(byte);

        public BizTransaction(TransactionType type)
            : base(type)
        {
        }

        protected abstract void DeserializeBizData(BinaryReader reader);
        protected abstract void SerializeBizData(BinaryWriter writer);
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            BizScriptHash = reader.ReadSerializable<UInt160>();
            Flag = reader.ReadByte();
            DeserializeBizData(reader);
        }
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(BizScriptHash);
            writer.Write(Flag);
            SerializeBizData(writer);
        }

    }
}
