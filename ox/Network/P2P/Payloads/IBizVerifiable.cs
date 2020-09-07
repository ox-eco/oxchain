using OX.IO;
using OX.Persistence;
using OX.VM;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public interface IBizVerifiable : ISerializable
    {
        void DeserializeUnsigned(BinaryReader reader);
        void SerializeUnsigned(BinaryWriter writer);
       
    }
}
