using OX.Cryptography.ECC;
using OX.IO;
using System.IO;
using OX.Ledger;

namespace OX.Network.P2P.Payloads
{
    public class ChainPosition : ISerializable
    {
        public uint BlockIndex;
        public ushort TxIndex;
      
        public virtual int Size => sizeof(uint) + sizeof(ushort) ;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(BlockIndex);
            writer.Write(TxIndex);
        }
        public void Deserialize(BinaryReader reader)
        {
            BlockIndex = reader.ReadUInt32();
            TxIndex = reader.ReadUInt16();
        }
    }
}
