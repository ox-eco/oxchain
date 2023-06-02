using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using OX.Wallets;
using OX.Cryptography.ECC;
using OX.SmartContract;
using OX.Ledger;
using OX.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using System;

namespace OX.Network.P2P.Payloads
{
    public enum NftChangeType : byte
    {
        Issue = 1 << 0,
        Transfer = 1 << 1
    }
    public class NftID : IEquatable<NftID>, ISerializable
    {
        public string CID;
        /// <summary>
        /// 0:IPFS CID is ipfs cid
        /// 1:other CID is file hash string
        /// </summary>
        public byte IDType;
        public int Size => CID.GetVarSize() + sizeof(byte);
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
            writer.WriteVarString(CID);
            writer.Write(IDType);

        }
        public void Deserialize(BinaryReader reader)
        {
            CID = reader.ReadVarString();
            IDType = reader.ReadByte();
        }
        public bool Equals(NftID other)
        {
            if (other.IsNull())
                return false;
            return other.CID.ToLower() == this.CID.ToLower() && other.IDType == this.IDType;
        }
    }
    public class NftCoinCopyright : ISerializable
    {
        public NftID NftID;
        public byte Flag;
        public string NftName;
        public string Description;
        public string Seal;
        public string AuthorName;
        public int Size => NftID.Size + sizeof(byte) + NftName.GetVarSize() + Description.GetVarSize() + Seal.GetVarSize() + AuthorName.GetVarSize();
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
            writer.Write(NftID);
            writer.Write(Flag);
            writer.WriteVarString(NftName);
            writer.WriteVarString(Description);
            writer.WriteVarString(Seal);
            writer.WriteVarString(AuthorName);

        }
        public void Deserialize(BinaryReader reader)
        {
            NftID = reader.ReadSerializable<NftID>();
            Flag = reader.ReadByte();
            NftName = reader.ReadVarString();
            Description = reader.ReadVarString();
            Seal = reader.ReadVarString();
            AuthorName = reader.ReadVarString();
        }
    }
    public class NftTransferCopyright : ISerializable
    {
        public byte CopyrightType;
        public string SN;
        public string HolderName;
        public int Size => sizeof(byte) + SN.GetVarSize() + HolderName.GetVarSize();
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
            writer.Write(CopyrightType);
            writer.WriteVarString(SN);
            writer.WriteVarString(HolderName);

        }
        public void Deserialize(BinaryReader reader)
        {
            CopyrightType = reader.ReadByte();
            SN = reader.ReadVarString();
            HolderName = reader.ReadVarString();
        }
    }
    public class NftTransaction : Transaction
    {
        public NftCoinCopyright NftCopyright;
        public ECPoint Author;
        /// <summary>
        /// 0:Image,1:Music,2:Video
        /// </summary>
        public byte ContentType;
        /// <summary>
        ///  Lockup period for first resale,N*10000 blocks
        /// </summary>
        public byte FirstResaleLock;
        public byte[] Mark;

        public override int Size => base.Size + NftCopyright.Size + Author.Size + sizeof(byte) + sizeof(byte) + Mark.GetVarSize();
        public NftTransaction()
          : base(TransactionType.NftTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            Mark = new byte[0];
        }
        public NftTransaction(ECPoint author)
            : this()
        {
            Author = author;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return Contract.CreateSignatureRedeemScript(this.Author).ToScriptHash();
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            NftCopyright = reader.ReadSerializable<NftCoinCopyright>();
            Author = reader.ReadSerializable<ECPoint>();
            ContentType = reader.ReadByte();
            FirstResaleLock = reader.ReadByte();
            Mark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(NftCopyright);
            writer.Write(Author);
            writer.Write(ContentType);
            writer.Write(FirstResaleLock);
            writer.WriteVarBytes(Mark);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["cid"] = this.NftCopyright.NftID.CID;
            json["idtype"] = this.NftCopyright.NftID.IDType.ToString();
            json["seal"] = this.NftCopyright.Seal;
            json["authorname"] = this.NftCopyright.AuthorName;
            json["author"] = Author.ToString();
            json["contenttype"] = ContentType.Value();
            json["subcontenttype"] = FirstResaleLock.Value();
            json["mark"] = Mark.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.NftCopyright.IsNull()) return false;
            if (this.NftCopyright.NftID.IsNull()) return false;
            if (this.NftCopyright.NftID.CID.IsNullOrEmpty()) return false;
            if (this.Author.IsNull()) return false;
            if (snapshot.NFTs.TryGet(this.NftCopyright.NftID).IsNotNull()) return false;
            return base.Verify(snapshot, mempool);
        }
    }
    public class NftTransferTransaction : Transaction
    {
        public NFSStateKey NFSStateKey;
        public NftChangeType NftChangeType;
        public NftTransferCopyright NFSCopyright;
        public NFSHolder NFSHolder;
        public MixSignatureValidator<NftTransferAuthentication> Auth;
        public override int Size => base.Size + NFSStateKey.Size + sizeof(NftChangeType) + NFSCopyright.Size + NFSHolder.Size + sizeof(bool) + (Auth.IsNotNull() ? Auth.Size : 0);
        public NftTransferTransaction()
          : base(TransactionType.NftTransferTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }


        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            NFSStateKey = reader.ReadSerializable<NFSStateKey>();
            NftChangeType = (NftChangeType)reader.ReadByte();
            NFSCopyright = reader.ReadSerializable<NftTransferCopyright>();
            NFSHolder = reader.ReadSerializable<NFSHolder>();
            if (reader.ReadBoolean())
                Auth = reader.ReadSerializable<MixSignatureValidator<NftTransferAuthentication>>();

        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(NFSStateKey);
            writer.Write((byte)NftChangeType);
            writer.Write(NFSCopyright);
            writer.Write(NFSHolder);
            if (Auth.IsNotNull())
            {
                writer.Write(true);
                writer.Write(Auth);
            }
            else
            {
                writer.Write(false);
            }
        }


        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            if (this.NftChangeType == NftChangeType.Issue && this.NFSStateKey.IsNotNull() && this.NFSStateKey.NFCID.IsNotNull())
            {
                var nfcstate = Blockchain.Singleton.CurrentSnapshot.GetNftState(this.NFSStateKey.NFCID);
                if (nfcstate.IsNotNull())
                    yield return Contract.CreateSignatureRedeemScript(nfcstate.NFC.Author).ToScriptHash();
            }
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (NFSStateKey.IsNull()) return false;
            if (NFSStateKey.NFCID.IsNull()) return false;
            if (NFSCopyright.IsNull()) return false;
            if (NFSHolder.IsNull()) return false;
            if (!NFSHolder.Verify()) return false;
            var nfcState = snapshot.GetNftState(NFSStateKey.NFCID);
            if (nfcState.IsNull()) return false;
            if (this.NftChangeType == NftChangeType.Transfer)
            {
                if (Auth.IsNull()) return false;
                if (Auth.Target.IsNull()) return false;
                if (Auth.Signature.IsNullOrEmpty()) return false;
                if (!Auth.Verify()) return false;

                var nfsState = snapshot.GetNftTransfer(this.NFSStateKey);
                if (nfsState.IsNull()) return false;
                if (!nfsState.LastNFS.NFSHolder.Equals(Auth.Target.Target)) return false;
                if (nfsState.LastNFS.Hash != Auth.Target.PreHash) return false;
                var height = snapshot.Height;
                if (nfsState.LastNFS.NftChangeType == NftChangeType.Issue && nfcState.NFC.FirstResaleLock > 0)
                {
                    if (height <= this.NFSStateKey.IssueBlockIndex + nfcState.NFC.FirstResaleLock * 10000) return false;
                }

                UInt160 oldOwner = default;
                if (nfsState.LastNFS.NFSHolder.MixAccountType == MixAccountType.OX)
                {
                    oldOwner = nfsState.LastNFS.NFSHolder.AsOXAddress();
                }
                else if (nfsState.LastNFS.NFSHolder.MixAccountType == MixAccountType.Ethereum)
                {
                    oldOwner = nfsState.LastNFS.NFSHolder.AsEthAddress().BuildMapAddress();
                }
                else return false;
                if (this.Outputs.IsNullOrEmpty()) return false;
                var outputs = this.Outputs.Where(m => m.AssetId.Equals(Blockchain.OXC) && m.ScriptHash.Equals(oldOwner));
                if (outputs.IsNullOrEmpty()) return false;
                if (outputs.Sum(m => m.Value) < Auth.Target.Amount) return false;
                if (Auth.Target.MaxIndex > 0)
                {
                    if (Auth.Target.MaxIndex <= height) return false;
                }
                if (Auth.Target.MinIndex > 0)
                {
                    if (Auth.Target.MinIndex > height + 1) return false;
                }
            }
            return base.Verify(snapshot, mempool);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();

            return json;
        }
    }
}
