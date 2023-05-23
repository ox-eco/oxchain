using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Cryptography;
using OX.Network.P2P;
using System;
using System.IO;

namespace OX.Ledger
{

    public class NFCState : StateBase, ICloneable<NFCState>
    {
        public NftTransaction NFC;
        public uint BlockIndex;
        public ushort N;


        public override int Size => base.Size + NFC.Size + sizeof(uint) + sizeof(ushort);

        NFCState ICloneable<NFCState>.Clone()
        {
            return new NFCState
            {
                NFC = NFC,
                BlockIndex = BlockIndex,
                N = N
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var coin = Transaction.DeserializeFrom(reader);
            if (coin.IsNotNull() && coin is NftTransaction nftcoint)
                this.NFC = nftcoint;
            this.BlockIndex = reader.ReadUInt32();
            this.N = reader.ReadUInt16();
        }

        void ICloneable<NFCState>.FromReplica(NFCState replica)
        {
            this.NFC = replica.NFC;
            this.BlockIndex = replica.BlockIndex;
            this.N = replica.N;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.NFC);
            writer.Write(BlockIndex);
            writer.Write(N);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            if (NFC.IsNotNull())
            {
                json["nfc"] = NFC.ToJson();
            }
            json["blockindex"] = BlockIndex.ToString();
            json["n"] = N.ToString();
            return json;
        }
    }
    public class NFSStateKey : IEquatable<NFSStateKey>, ISerializable
    {
        public NftID NFCID;
        public uint IssueBlockIndex;
        public ushort IssueN;
        public int Size => NFCID.Size + sizeof(uint) + sizeof(ushort);
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
            writer.Write(NFCID);
            writer.Write(IssueBlockIndex);
            writer.Write(IssueN);
        }
        public void Deserialize(BinaryReader reader)
        {
            NFCID = reader.ReadSerializable<NftID>();
            IssueBlockIndex = reader.ReadUInt32();
            IssueN = reader.ReadUInt16();
        }
        public bool Equals(NFSStateKey other)
        {
            if (other is null)
                return false;
            return this.NFCID == other.NFCID && this.IssueBlockIndex == other.IssueBlockIndex && this.IssueN == other.IssueN;
        }

    }
    public class NFSState : StateBase, ICloneable<NFSState>
    {
        public NftTransferTransaction LastNFS;
        public uint TransferBlockIndex;
        public ushort TransferN;

        public override int Size => base.Size + LastNFS.Size + sizeof(uint) + sizeof(ushort);

        NFSState ICloneable<NFSState>.Clone()
        {
            return new NFSState
            {
                LastNFS = LastNFS,
                TransferBlockIndex = TransferBlockIndex,
                TransferN = TransferN
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var tx = Transaction.DeserializeFrom(reader);
            if (tx.IsNotNull() && tx is NftTransferTransaction nfstx)
                this.LastNFS = nfstx;
            this.TransferBlockIndex = reader.ReadUInt32();
            this.TransferN = reader.ReadUInt16();
        }

        void ICloneable<NFSState>.FromReplica(NFSState replica)
        {
            this.LastNFS = replica.LastNFS;
            this.TransferBlockIndex = replica.TransferBlockIndex;
            this.TransferN = replica.TransferN;

        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(LastNFS);
            writer.Write(TransferBlockIndex);
            writer.Write(TransferN);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["nfstx"] = LastNFS.ToJson();
            json["transferblockindex"] = TransferBlockIndex.ToString();
            json["transfern"] = TransferN.ToString();
            return json;
        }
    }
}
