using OX.Cryptography;
using OX.IO;
using OX.Network.P2P.Payloads;
using OX.Wallets;
using System.IO;
using System.Linq;

namespace OX.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this ISerializable serializable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                serializable.Serialize(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] Sign(this ISerializable serializable, KeyPair key)
        {
            return Crypto.Default.Sign(serializable.GetHashData(), key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
        }
        public static byte[] Sign(this byte[] Data, KeyPair key)
        {
            return Crypto.Default.Sign(Data, key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
        }
    }
}
