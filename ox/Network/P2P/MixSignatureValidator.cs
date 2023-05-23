using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.Network.P2P.Payloads;
using OX.IO;
using System.IO;
using System.Linq;
using System.Text;
using Nethereum.Util;
using Nethereum.Hex.HexConvertors.Extensions;
using OX.SmartContract;

namespace OX.Network.P2P
{
    public enum MixAccountType : byte
    {
        OX = 1 << 0,
        Ethereum = 1 << 1
    }
    public interface IVerify : ISerializable
    {
        bool Verify();
    }

    public interface IMixSignatureTarget : ISerializable
    {
        NFSHolder Target { get; }
    }
    public class MixSignatureValidator<T> : IVerify where T : IMixSignatureTarget, new()
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
            if (Target.Target.Verify())
            {
                if (Target.Target.MixAccountType == MixAccountType.OX)
                {
                    return Crypto.Default.VerifySignature(Target.ToArray(), Signature, Target.Target.Target);
                }
                else if (Target.Target.MixAccountType == MixAccountType.Ethereum)
                {
                    try
                    {
                        var stringToSign = Target.ToArray().ToHexString();
                        var signatureData = Encoding.UTF8.GetString(Signature);
                        var signer = new Nethereum.Signer.EthereumMessageSigner();
                        var ethaddress = signer.EncodeUTF8AndEcRecover(stringToSign, signatureData);
                        //ethAddress.HexToByteArray()
                        var address = new AddressUtil().ConvertToChecksumAddress(Target.Target.Target.ToHex());
                        if (ethaddress.ToLower() == address.ToLower()) return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
    public class NftTransferAuthentication : IMixSignatureTarget
    {
        public NFSHolder Target { get; set; }
        public UInt256 PreHash;
        public Fixed8 Amount;
        public uint MinIndex;
        public uint MaxIndex;
        public virtual int Size => Target.Size + PreHash.Size + Amount.Size + sizeof(uint) + sizeof(uint);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Target);
            writer.Write(PreHash);
            writer.Write(Amount);
            writer.Write(MinIndex);
            writer.Write(MaxIndex);
        }
        public void Deserialize(BinaryReader reader)
        {
            Target = reader.ReadSerializable<NFSHolder>();
            PreHash = reader.ReadSerializable<UInt256>();
            Amount = reader.ReadSerializable<Fixed8>();
            MinIndex = reader.ReadUInt32();
            MaxIndex = reader.ReadUInt32();
        }
    }
    public class NFSHolder : IVerify
    {
        public MixAccountType MixAccountType { get; set; }
        public byte[] Target { get; set; }

        public virtual int Size => sizeof(MixAccountType) + Target.GetVarSize();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)MixAccountType);
            writer.WriteVarBytes(Target);

        }
        public void Deserialize(BinaryReader reader)
        {
            MixAccountType = (MixAccountType)reader.ReadByte();
            Target = reader.ReadVarBytes();
        }
        public bool Verify()
        {
            if (MixAccountType == MixAccountType.OX)
            {
                try
                {
                    ECPoint.DecodePoint(this.Target, ECCurve.Secp256r1);
                }
                catch
                {
                    return false; ;
                }
            }
            else if (MixAccountType == MixAccountType.Ethereum)
            {
                try
                {
                    var addr = new AddressUtil().ConvertToChecksumAddress(this.Target.ToHex());
                    return addr.IsValidEthereumAddressHexFormat();
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        public string AsEthAddress()
        {
            return new AddressUtil().ConvertToChecksumAddress(Target.ToHex());
        }
        public UInt160 AsOXAddress()
        {
            return Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(this.Target, ECCurve.Secp256r1)).ToScriptHash();
        }
        public override bool Equals(object obj)
        {
            if (obj is NFSHolder holder)
            {
                if (holder.Target.Length != this.Target.Length) return false;
                return holder.Target.SequenceEqual(Target) && holder.MixAccountType == MixAccountType;
            }
            return base.Equals(obj);
        }
    }
}
