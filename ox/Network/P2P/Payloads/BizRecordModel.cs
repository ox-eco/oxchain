using OX.IO;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public abstract class BizModel : ISerializable
    {
        public byte Prefix;
        public virtual int Size => sizeof(byte);
        public BizModel(byte prefix)
        {
            this.Prefix = prefix;
        }
        public abstract void SerializeBizModel(BinaryWriter writer);
        public abstract void DeserializeBizModel(BinaryReader reader);
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Prefix);
            SerializeBizModel(writer);
        }
        public void Deserialize(BinaryReader reader)
        {
            Prefix = reader.ReadByte();
            DeserializeBizModel(reader);
        }
    }
    public sealed class BizRecordModel
    {
        public UInt160 ScriptHash { get; set; }
        public byte[] Key { get; set; }
        public BizModel Model { get; set; }
    }
}
