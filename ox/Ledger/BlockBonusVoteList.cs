using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Json;
using System.IO;
using System.Linq;

namespace OX.Ledger
{
    public class BlockBonusVote : ISerializable
    {
        public ECPoint Voter;
        public byte NumPerBlock;
        public Fixed8 Amount;
        public virtual int Size => Voter.Size + sizeof(byte) + Amount.Size;
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Voter);
            writer.Write(NumPerBlock);
            writer.Write(Amount);
        }
        public void Deserialize(BinaryReader reader)
        {
            Voter = reader.ReadSerializable<ECPoint>();
            NumPerBlock = reader.ReadByte();
            Amount = reader.ReadSerializable<Fixed8>();
        }
    }
    public class BlockBonusVoteList : StateBase, ICloneable<BlockBonusVoteList>
    {
        public BlockBonusVote[] Votes;

        public override int Size => base.Size + Votes.GetVarSize();

        BlockBonusVoteList ICloneable<BlockBonusVoteList>.Clone()
        {
            return new BlockBonusVoteList
            {
                Votes = Votes
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Votes = reader.ReadSerializableArray<BlockBonusVote>();
        }

        void ICloneable<BlockBonusVoteList>.FromReplica(BlockBonusVoteList replica)
        {
            Votes = replica.Votes;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Votes);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["votes"] = Votes.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
