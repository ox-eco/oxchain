using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OX.IO;
using Org.BouncyCastle.Asn1.Cms;

namespace OX.Cryptography
{
    public class EncryptData : ISerializable
    {
        public byte[] Data;
        public virtual int Size => Data.GetVarSize();
        public EncryptData()
        {

        }
        public EncryptData(byte[] data) : this()
        {
            this.Data = data;
        }
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data);
        }
        public virtual void Deserialize(BinaryReader reader)
        {
            Data = reader.ReadVarBytes();
        }
    }
}
