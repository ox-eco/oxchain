using OX.IO;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public interface IBizVerifiable : ISerializable
    {
        void DeserializeUnsigned(BinaryReader reader);
        void SerializeUnsigned(BinaryWriter writer);

    }
}
