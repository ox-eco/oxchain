using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Cryptography;
using OX.Network.P2P;
using System;
using System.IO;

namespace OX.Ledger
{
    public class NFTDonateStateKey : IEquatable<NFTDonateStateKey>, ISerializable
    {
        public UInt256 NFTCoinHash;
        public uint IssueBlockIndex;
        public ushort IssueN;
        public UInt256 IssueDonateHash;
        public int Size => NFTCoinHash.Size + sizeof(uint) + sizeof(ushort) + sizeof(byte) + IssueDonateHash.Size;
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
            writer.Write(NFTCoinHash);
            writer.Write(IssueBlockIndex);
            writer.Write(IssueN);
            if (IssueDonateHash.IsNotNull())
            {
                writer.Write((byte)1);
                writer.Write(IssueDonateHash);
            }
            else
                writer.Write((byte)0);

        }
        public void Deserialize(BinaryReader reader)
        {
            NFTCoinHash = reader.ReadSerializable<UInt256>();
            IssueBlockIndex = reader.ReadUInt32();
            IssueN = reader.ReadUInt16();
            if (reader.ReadByte() > 0)
                IssueDonateHash = reader.ReadSerializable<UInt256>();
        }
        public bool Equals(NFTDonateStateKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            bool ok = IssueDonateHash.IsNull() || IssueDonateHash.Equals(other.IssueDonateHash);
            return ok && NFTCoinHash.Equals(other.NFTCoinHash) && IssueBlockIndex == other.IssueBlockIndex && IssueN == other.IssueN;
        }

    }
    public class NFTState : StateBase, ICloneable<NFTState>
    {
        public NFTCoinTransaction NFTCoin;
        public uint BlockIndex;
        public ushort N;
        public UInt256 DataHash;

        public override int Size => base.Size + NFTCoin.Size + sizeof(uint) + sizeof(ushort) + DataHash.Size;

        NFTState ICloneable<NFTState>.Clone()
        {
            return new NFTState
            {
                NFTCoin = NFTCoin,
                BlockIndex = BlockIndex,
                N = N,
                DataHash = DataHash
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var coin = Transaction.DeserializeFrom(reader);
            if (coin.IsNotNull() && coin is NFTCoinTransaction nftcoint)
                this.NFTCoin = nftcoint;
            this.BlockIndex = reader.ReadUInt32();
            this.N = reader.ReadUInt16();
            this.DataHash = reader.ReadSerializable<UInt256>();
        }

        void ICloneable<NFTState>.FromReplica(NFTState replica)
        {
            this.NFTCoin = replica.NFTCoin;
            this.BlockIndex = replica.BlockIndex;
            this.N = replica.N;
            this.DataHash = replica.DataHash;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(NFTCoin);
            writer.Write(BlockIndex);
            writer.Write(N);
            writer.Write(DataHash);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            if (NFTCoin.IsNotNull())
            {
                json["nftcoint"] = NFTCoin.ToJson();
            }
            json["blockindex"] = BlockIndex.ToString();
            json["n"] = N.ToString();
            json["datahash"] = DataHash.ToString();
            return json;
        }
    }
    public class NFTDonateState : StateBase, ICloneable<NFTDonateState>
    {
        public NFTDonateTransaction IssueTx;
        public uint TransferBlockIndex;
        public ushort TransferN;
        public NFTDonateTransaction TransferTx;

        public override int Size => base.Size + IssueTx.Size + sizeof(uint) + sizeof(ushort) + sizeof(byte) + TransferTx.Size;

        NFTDonateState ICloneable<NFTDonateState>.Clone()
        {
            return new NFTDonateState
            {
                IssueTx = IssueTx,
                TransferBlockIndex = TransferBlockIndex,
                TransferN = TransferN,
                TransferTx = TransferTx
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var issue = Transaction.DeserializeFrom(reader);
            if (issue.IsNotNull() && issue is NFTDonateTransaction issueTx)
                this.IssueTx = issueTx;
            this.TransferBlockIndex = reader.ReadUInt32();
            this.TransferN = reader.ReadUInt16();
            if (reader.ReadByte() > 0)
            {
                var transfer = Transaction.DeserializeFrom(reader);
                if (transfer.IsNotNull() && transfer is NFTDonateTransaction transferTx)
                    this.TransferTx = transferTx;
            }
        }

        void ICloneable<NFTDonateState>.FromReplica(NFTDonateState replica)
        {
            this.IssueTx = replica.IssueTx;
            this.TransferTx = replica.TransferTx;
            this.TransferBlockIndex = replica.TransferBlockIndex;
            this.TransferN = replica.TransferN;

        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(IssueTx);
            writer.Write(TransferBlockIndex);
            writer.Write(TransferN);
            if (this.TransferTx.IsNotNull())
            {
                writer.Write((byte)1);
                writer.Write(this.TransferTx);
            }
            else
            {
                writer.Write((byte)0);
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["issuetx"] = IssueTx.ToJson();
            if (TransferTx.IsNotNull())
            {
                json["transferblockindex"] = TransferBlockIndex.ToString();
                json["transfern"] = TransferN.ToString();
                json["transfertx"] = TransferTx.ToJson();
            }
            return json;
        }
    }
}
