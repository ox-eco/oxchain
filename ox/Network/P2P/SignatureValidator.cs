using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OX.IO;
using OX.IO.Json;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Cryptography.ECC;
using OX.Cryptography;
using OX.SmartContract;

namespace OX.Network.P2P
{
    public interface ISignatureTarget : ISerializable
    {
        ECPoint PublicKey { get; }
    }
    public class SignatureValidator<T> : ISerializable where T : ISignatureTarget, new()
    {
        public T Target;
        public byte[] Signature;
        public int Size => Target.Size + Signature.GetVarSize();
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
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Target);
            writer.WriteVarBytes(Signature);
        }
        public void Deserialize(BinaryReader reader)
        {
            Target = reader.ReadSerializable<T>();
            Signature = reader.ReadVarBytes();
        }
        public bool Verify()
        {
            return Crypto.Default.VerifySignature(Target.ToArray(), Signature, Target.PublicKey.EncodePoint(true));
        }
    }
}
