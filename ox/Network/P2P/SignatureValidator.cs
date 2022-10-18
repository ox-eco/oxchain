using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.Network.P2P.Payloads;
using OX.IO;
using System.IO;
using System.Linq;

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

    public class NFTDonateAuthentication : ISignatureTarget
    {
        public ECPoint PublicKey { get; set; }
        public NFTDonateType NFTDonateType;
        public UInt256 PreHash;
        public UInt160 NewOwner;
        public Fixed8 Amount;
        public virtual int Size => PublicKey.Size + sizeof(NFTDonateType) + PreHash.Size  + (NFTDonateType == NFTDonateType.Sell ? Amount.Size : NewOwner.Size);
        public NFTDonateAuthentication()
        {
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PublicKey);
            writer.Write((byte)NFTDonateType);
            writer.Write(PreHash);
            if (NFTDonateType == NFTDonateType.Sell)
                writer.Write(Amount);
            else
                writer.Write(NewOwner);
        }
        public void Deserialize(BinaryReader reader)
        {
            PublicKey = reader.ReadSerializable<ECPoint>();
            NFTDonateType = (NFTDonateType)reader.ReadByte();
            PreHash = reader.ReadSerializable<UInt256>();
            if (NFTDonateType == NFTDonateType.Sell)
                Amount = reader.ReadSerializable<Fixed8>();
            else
                NewOwner = reader.ReadSerializable<UInt160>();
        }
    }
}
