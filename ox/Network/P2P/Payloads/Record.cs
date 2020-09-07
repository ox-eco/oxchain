using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OX.IO;
using OX.IO.Caching;
using OX.IO.Json;
using OX.Cryptography;
using OX.Cryptography.ECC;

namespace OX.Network.P2P.Payloads
{

    public sealed class Record : ISerializable
    {
        public UInt160 ScriptHash;
        public byte Prefix;
        public byte[] Key;
        public byte[] Data;
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
        public int Size => ScriptHash.Size + sizeof(byte) + Key.GetVarSize() + Data.GetVarSize();

        public Record()
        { }
        public Record(UInt160 scriptHash, byte prefix) : this()
        {
            this.ScriptHash = scriptHash;
            this.Prefix = prefix;
        }
        void ISerializable.Deserialize(BinaryReader reader)
        {
            ScriptHash = reader.ReadSerializable<UInt160>();
            Prefix = reader.ReadByte();
            Key = reader.ReadVarBytes();
            Data = reader.ReadVarBytes();
        }


        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write(Prefix);
            writer.WriteVarBytes(Key);
            writer.WriteVarBytes(Data);
        }


        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["scripthash"] = ScriptHash.ToArray().ToHexString();
            json["prefix"] = Prefix.ToString();
            json["key"] = Key.ToHexString();
            json["data"] = Data.ToHexString();
            return json;
        }

    }
}
