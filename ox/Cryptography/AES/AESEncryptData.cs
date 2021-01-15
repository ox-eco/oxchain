using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OX.IO;

namespace OX.Cryptography.AES
{
    public class AESEncryptData : ISerializable
    {
        public byte[] Data;
        public virtual int Size => Data.GetVarSize();
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
