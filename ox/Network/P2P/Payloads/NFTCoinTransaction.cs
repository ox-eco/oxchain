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

namespace OX.Network.P2P.Payloads
{
    public enum NFTCoinType : byte
    {
        OnChain = 1 << 0,
        OutChain = 1 << 1,
        MixChain = 1 << 2
    }
    public enum NFTDonatePermission : byte
    {
        AllowDonate = 1 << 0,
        NoDonate = 1 << 1
    }
    public enum NFTDonateType : byte
    {
        Issue = 1 << 0,
        Transfer = 1 << 1,
        Sell = 1 << 2
    }
    public class NFTCopyright : ISerializable
    {
        public byte CopyrightType;
        public string IdentityNo;
        public string FullName;
        public int Size => sizeof(byte) + IdentityNo.GetVarSize() + FullName.GetVarSize();
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
            writer.WriteVarString(IdentityNo);
            writer.WriteVarString(FullName);

        }
        public void Deserialize(BinaryReader reader)
        {
            CopyrightType = reader.ReadByte();
            IdentityNo = reader.ReadVarString();
            FullName = reader.ReadVarString();
        }
    }
    public class NFTCoinTransaction : Transaction
    {
        public ECPoint Author;
        public NFTCoinType NFTType;
        public NFTDonatePermission NFTDonatePermission;
        /// <summary>
        /// 0:Image,1:Music,2:Video
        /// </summary>
        public byte IssueType;
        /// <summary>
        ///  IssueType==0,0:jpg,1:gif,2:png,3:bmp
        /// </summary>
        public byte SubIssueType;
        public byte Flag;
        public byte[] Data;
        public byte[] Format;
        public byte[] Mark;

        public override int Size => base.Size + Author.Size + sizeof(NFTCoinType) + sizeof(NFTDonatePermission) + sizeof(byte) + sizeof(byte) + sizeof(byte) + Data.GetVarSize() + Format.GetVarSize() + Mark.GetVarSize();

        private UInt256 _datahash = null;
        public UInt256 DataHash
        {
            get
            {
                if (_datahash == null)
                {
                    _datahash = new UInt256(Crypto.Default.Hash256(Data));
                }
                return _datahash;
            }
        }
        public NFTCoinTransaction()
          : base(TransactionType.NFTCoinTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            Data = new byte[0];
            Format = new byte[0];
            Mark = new byte[0];
        }
        public NFTCoinTransaction(ECPoint author)
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
            Author = reader.ReadSerializable<ECPoint>();
            NFTType = (NFTCoinType)reader.ReadByte();
            NFTDonatePermission = (NFTDonatePermission)reader.ReadByte();
            IssueType = reader.ReadByte();
            SubIssueType = reader.ReadByte();
            Flag = reader.ReadByte();
            Data = reader.ReadVarBytes();
            Format = reader.ReadVarBytes();
            Mark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Author);
            writer.Write((byte)NFTType);
            writer.Write((byte)NFTDonatePermission);
            writer.Write(IssueType);
            writer.Write(SubIssueType);
            writer.Write(Flag);
            writer.WriteVarBytes(Data);
            writer.WriteVarBytes(Format);
            writer.WriteVarBytes(Mark);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["author"] = Author.ToString();
            json["nfttype"] = NFTType.Value();
            json["nftdonatepermission"] = NFTDonatePermission.Value();
            json["issuetype"] = IssueType.Value();
            json["subissuetype"] = SubIssueType.Value();
            json["flag"] = Flag.Value();
            json["data"] = Data.ToHexString();
            json["format"] = Format.ToHexString();
            json["mark"] = Mark.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Author.IsNull()) return false;
            if (this.Data.IsNullOrEmpty()) return false;
            return base.Verify(snapshot, mempool);
        }
    }
    public class NFTDonateTransaction : Transaction
    {
        public NFTCopyright CopyRight;
        public NFTDonateStateKey NFTDonateStateKey;
        public SignatureValidator<NFTDonateAuthentication> DonateAuthentication;
        public uint SN;
        public UInt160 NewOwner;
        public UInt160 NFTOwner
        {
            get
            {
                return this.DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell ? this.NewOwner : this.DonateAuthentication.Target.NewOwner;
            }
        }

        public override int Size => base.Size + CopyRight.Size + NFTDonateStateKey.Size + DonateAuthentication.Size + sizeof(uint) + (DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell ? this.NewOwner.Size : 0);

        public NFTDonateTransaction()
          : base(TransactionType.NFTDonateTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
        }
        public NFTDonateTransaction(NFTCopyright copyright, NFTDonateStateKey key, SignatureValidator<NFTDonateAuthentication> auth)
            : this()
        {
            this.CopyRight = copyright;
            this.NFTDonateStateKey = key;
            this.DonateAuthentication = auth;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            if (this.DonateAuthentication.Target.NFTDonateType != NFTDonateType.Sell)
                yield return this.DonateAuthentication.Target.NewOwner;
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            CopyRight = reader.ReadSerializable<NFTCopyright>();
            NFTDonateStateKey = reader.ReadSerializable<NFTDonateStateKey>();
            DonateAuthentication = reader.ReadSerializable<SignatureValidator<NFTDonateAuthentication>>();
            SN = reader.ReadUInt32();
            if (DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell)
                NewOwner = reader.ReadSerializable<UInt160>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(CopyRight);
            writer.Write(NFTDonateStateKey);
            writer.Write(DonateAuthentication);
            writer.Write(SN);
            if (DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell)
                writer.Write(NewOwner);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["copyright"] = "0";
            json["nftdonatestatekey"] = "0";
            json["donateauthentication"] = "0";
            json["sn"] = SN.ToString();
            if (DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell)
                json["newowner"] = NewOwner.ToAddress();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (!this.DonateAuthentication.Verify()) return false;
            if (this.DonateAuthentication.Target.NFTDonateType == NFTDonateType.Issue)
            {
                if (!this.NFTDonateStateKey.NFTCoinHash.Equals(this.DonateAuthentication.Target.PreHash)) return false;
                var nftstate = Blockchain.Singleton.Store.GetNFTState(this.DonateAuthentication.Target.PreHash);
                if (nftstate.IsNull()) return false;
                if (!this.NFTDonateStateKey.NFTCoinHash.Equals(nftstate.NFTCoin.Hash)) return false;
                if (!nftstate.NFTCoin.Author.Equals(this.DonateAuthentication.Target.PublicKey)) return false;
            }
            else
            {
                if (this.NFTDonateStateKey.IssueDonateHash.IsNull()) return false;
                if (this.NFTDonateStateKey.IssueBlockIndex == 0) return false;
                if (this.NFTDonateStateKey.IssueN == 0) return false;
                var donateState = Blockchain.Singleton.Store.GetNFTDonateState(this.NFTDonateStateKey);
                if (donateState.IsNull()) return false;
                var nftState = Blockchain.Singleton.Store.GetNFTState(donateState.IssueTx.NFTDonateStateKey.NFTCoinHash);
                if (nftState.IsNull()) return false;
                if (nftState.NFTCoin.NFTDonatePermission == NFTDonatePermission.NoDonate) return false;
                NFTDonateTransaction oldDonateTx = donateState.TransferTx.IsNotNull() ? donateState.TransferTx : donateState.IssueTx;
                if (!oldDonateTx.Hash.Equals(this.DonateAuthentication.Target.PreHash)) return false;
                UInt160 oldOwner = oldDonateTx.NFTOwner;
                var sh = Contract.CreateSignatureRedeemScript(this.DonateAuthentication.Target.PublicKey).ToScriptHash();
                if (!oldOwner.Equals(sh)) return false;
                if (this.DonateAuthentication.Target.NFTDonateType == NFTDonateType.Sell)
                {
                    if (this.NewOwner.IsNull()) return false;
                    if (this.Outputs.IsNullOrEmpty()) return false;
                    var outputs = this.Outputs.Where(m => m.AssetId.Equals(Blockchain.OXC) && m.ScriptHash.Equals(oldOwner));
                    if (outputs.IsNullOrEmpty()) return false;
                    if (outputs.Sum(m => m.Value) < this.DonateAuthentication.Target.Amount) return false;
                }
            }
            return base.Verify(snapshot, mempool);
        }
    }
}
