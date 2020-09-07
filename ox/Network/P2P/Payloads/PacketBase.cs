using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OX.IO;
using OX.IO.Json;
using OX.Cryptography;
using OX.VM;

namespace OX.Network.P2P.Payloads
{
  
    public abstract class PacketBase : IBizVerifiable
    {
        public uint Version;
        public byte BizID;
        public uint Height;
        public uint Index;
        public UInt256 PrevHash;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        public virtual int Size => sizeof(uint) + sizeof(byte) + sizeof(uint) + sizeof(uint) + PrevHash.Size;

        public virtual void Deserialize(BinaryReader reader)
        {
            ((IBizVerifiable)this).DeserializeUnsigned(reader);
        }


        public virtual void Serialize(BinaryWriter writer)
        {
            ((IBizVerifiable)this).SerializeUnsigned(writer);
        }
        void IBizVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(BizID);
            writer.Write(Height);
            writer.Write(Index);
            writer.Write(PrevHash);
        }
        void IBizVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            BizID = reader.ReadByte();
            Height = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
        }
        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["bizid"] = BizID.ToString();
            json["height"] = Height;
            json["index"] = Index;
            return json;
        }


    }
}
