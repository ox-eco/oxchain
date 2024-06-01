using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using OX.Cryptography.ECC;
using OX.SmartContract;
using OX.Ledger;
using OX.Cryptography;
using OX.VM;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace OX.Network.P2P.Payloads
{
    public enum LockAssetPurpose : byte
    {
        Common = 0x00,
        BlockBonusVote = 0x01,
        SlotOffVote = 0x02,
        DaoVote = 0x03
    }
    public interface ILockVote
    {
        uint Index { get; set; }
    }
    public class BlockBonusSetting : ISerializable, ILockVote
    {
        public uint Index { get; set; }
        public byte NumPerBlock;
        public virtual int Size => sizeof(uint) + sizeof(byte);
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(NumPerBlock);
        }
        public void Deserialize(BinaryReader reader)
        {
            Index = reader.ReadUInt32();
            NumPerBlock = reader.ReadByte();
        }
        public override bool Equals(object obj)
        {
            if (obj is BlockBonusSetting bbs)
            {
                return bbs.Index == this.Index && bbs.NumPerBlock == this.NumPerBlock;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(BlockBonusSetting left, BlockBonusSetting right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        public static bool operator !=(BlockBonusSetting left, BlockBonusSetting right)
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return (Index + NumPerBlock).GetHashCode();
        }
    }
    public class SlotOffVote : ISerializable, ILockVote
    {
        public UInt160 Slot;
        public uint Index { get; set; }
        public virtual int Size => Slot.Size + sizeof(uint);
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Slot);
            writer.Write(Index);
        }
        public void Deserialize(BinaryReader reader)
        {
            Slot = reader.ReadSerializable<UInt160>();
            Index = reader.ReadUInt32();
        }
        public override bool Equals(object obj)
        {
            if (obj is SlotOffVote bbs)
            {
                return bbs.Index == this.Index && bbs.Slot == this.Slot;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(SlotOffVote left, SlotOffVote right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        public static bool operator !=(SlotOffVote left, SlotOffVote right)
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return Index.GetHashCode() + Slot.GetHashCode();
        }
    }
    public class DaoVote : ISerializable, ILockVote
    {
        public UInt256 AssetId;
        public uint Index { get; set; }
        public virtual int Size => AssetId.Size + sizeof(uint);
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
            writer.Write(AssetId);
            writer.Write(Index);
        }
        public void Deserialize(BinaryReader reader)
        {
            AssetId = reader.ReadSerializable<UInt256>();
            Index = reader.ReadUInt32();
        }
        public override bool Equals(object obj)
        {
            if (obj is DaoVote bbs)
            {
                return bbs.Index == this.Index && bbs.AssetId == this.AssetId;
            }
            return base.Equals(obj);
        }
        public static bool operator ==(DaoVote left, DaoVote right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        public static bool operator !=(DaoVote left, DaoVote right)
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return Index.GetHashCode() + AssetId.GetHashCode();
        }
    }
    public class SlotOffVoteList : StateBase, ICloneable<SlotOffVoteList>
    {
        public Dictionary<uint, Fixed8> Votes;

        public override int Size => base.Size + IO.Helper.GetVarSize(Votes.Count) + Votes.Count * (sizeof(uint) + 8);

        public SlotOffVoteList()
        {
            Votes = new Dictionary<uint, Fixed8>();
        }
        SlotOffVoteList ICloneable<SlotOffVoteList>.Clone()
        {
            return new SlotOffVoteList
            {
                Votes = Votes
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            int count = (int)reader.ReadVarInt();
            Votes = new Dictionary<uint, Fixed8>(count);
            for (int i = 0; i < count; i++)
            {
                uint voteIndex = reader.ReadUInt32();
                Fixed8 value = reader.ReadSerializable<Fixed8>();
                Votes.Add(voteIndex, value);
            }
        }

        void ICloneable<SlotOffVoteList>.FromReplica(SlotOffVoteList replica)
        {
            Votes = replica.Votes;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            var vs = Votes.Where(p => p.Value > Fixed8.Zero).ToArray();
            writer.WriteVarInt(vs.Length);
            foreach (var pair in vs)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["votes"] = Votes.Select(p =>
            {
                JObject balance = new JObject();
                balance["voteIndex"] = p.Key.ToString();
                balance["value"] = p.Value.ToString();
                return balance;
            }).ToArray();
            return json;
        }
    }
    public class DaoVoteList : StateBase, ICloneable<DaoVoteList>
    {
        public Dictionary<uint, Fixed8> Votes;

        public override int Size => base.Size + IO.Helper.GetVarSize(Votes.Count) + Votes.Count * (sizeof(uint) + 8);

        public DaoVoteList()
        {
            Votes = new Dictionary<uint, Fixed8>();
        }
        DaoVoteList ICloneable<DaoVoteList>.Clone()
        {
            return new DaoVoteList
            {
                Votes = Votes
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            int count = (int)reader.ReadVarInt();
            Votes = new Dictionary<uint, Fixed8>(count);
            for (int i = 0; i < count; i++)
            {
                uint voteIndex = reader.ReadUInt32();
                Fixed8 value = reader.ReadSerializable<Fixed8>();
                Votes.Add(voteIndex, value);
            }
        }

        void ICloneable<DaoVoteList>.FromReplica(DaoVoteList replica)
        {
            Votes = replica.Votes;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            var vs = Votes.Where(p => p.Value > Fixed8.Zero).ToArray();
            writer.WriteVarInt(vs.Length);
            foreach (var pair in vs)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["votes"] = Votes.Select(p =>
            {
                JObject balance = new JObject();
                balance["voteIndex"] = p.Key.ToString();
                balance["value"] = p.Value.ToString();
                return balance;
            }).ToArray();
            return json;
        }
    }
}
