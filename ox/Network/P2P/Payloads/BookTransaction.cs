using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using OX.SmartContract;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class BookAuthor : ISerializable
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
    public class CopyrightOwner : ISerializable
    {
        public byte CopyrightType;
        public string IdentityNo;
        public string OwnerName;
        public int Size => sizeof(byte) + IdentityNo.GetVarSize() + OwnerName.GetVarSize();
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
            writer.WriteVarString(OwnerName);

        }
        public void Deserialize(BinaryReader reader)
        {
            CopyrightType = reader.ReadByte();
            IdentityNo = reader.ReadVarString();
            OwnerName = reader.ReadVarString();
        }
    }
    public class BookCopyrightAuthentication : ISignatureTarget
    {
        public ECPoint PublicKey { get; set; }
        public UInt256 BookId;
        public UInt160 NewOwner;
        public Fixed8 Amount;
        public uint MinIndex;
        public uint MaxIndex;
        public virtual int Size => PublicKey.Size + BookId.Size + NewOwner.Size + Amount.Size + sizeof(uint) + sizeof(uint);
        public BookCopyrightAuthentication()
        {
            this.NewOwner = UInt160.Zero;
            this.Amount = Fixed8.Zero;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PublicKey);
            writer.Write(BookId);
            writer.Write(NewOwner);
            writer.Write(Amount);
            writer.Write(MinIndex);
            writer.Write(MaxIndex);
        }
        public void Deserialize(BinaryReader reader)
        {
            PublicKey = reader.ReadSerializable<ECPoint>();
            BookId = reader.ReadSerializable<UInt256>();
            NewOwner = reader.ReadSerializable<UInt160>();
            Amount = reader.ReadSerializable<Fixed8>();
            MinIndex = reader.ReadUInt32();
            MaxIndex = reader.ReadUInt32();
        }
    }
    public class BookTransaction : Transaction
    {
        public ECPoint Author;
        public BookType BookType;
        public BookStorageType BookStorageType;
        public byte[] Data;
        public byte[] Copyright;

        public override int Size => base.Size + Author.Size + sizeof(BookType) + sizeof(BookStorageType) + Data.GetVarSize() + Copyright.GetVarSize();

        public BookTransaction()
          : base(TransactionType.BookTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Data = new byte[] { 0x00 };
            this.Copyright = new byte[] { 0x00 };
        }
        public BookTransaction(BookAuthor copyright) : this()
        {
            if (copyright.IsNotNull())
            {
                this.Copyright = copyright.ToArray();
            }
        }
        public BookAuthor GetBookCopyright()
        {
            if (this.Copyright.IsNotNullAndEmpty())
            {
                if (this.Copyright.Length == 1 && this.Copyright[0] == 0x00)
                {
                    return default;
                }
                try
                {
                    return this.Copyright.AsSerializable<BookAuthor>();
                }
                catch
                {
                    return default;
                }
            }
            return default;
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
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.Author.IsNull()) return false;
            if (this.Data.IsNullOrEmpty()) return false;
            if (this.Copyright.IsNullOrEmpty()) return false;
            return base.Verify(snapshot, mempool);
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Author = reader.ReadSerializable<ECPoint>();
            BookType = (BookType)reader.ReadByte();
            BookStorageType = (BookStorageType)reader.ReadByte();
            Data = reader.ReadVarBytes();
            Copyright = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Author);
            writer.Write((byte)BookType);
            writer.Write((byte)BookStorageType);
            writer.WriteVarBytes(Data);
            writer.WriteVarBytes(Copyright);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["author"] = Author.ToString();
            json["booktype"] = BookType.Value();
            json["data"] = Data.ToHexString();
            json["mark"] = Data.ToHexString();
            return json;
        }
    }
    public class BookSectionTransaction : Transaction
    {
        public UInt256 BookId;
        public Fixed8 FixedSerial;
        public BookSectionType BookSectionType;
        public BookEncodingType BookEncodingType;
        public UInt256 SectionHash;
        public byte[] Data;
        public byte Flag;
        public byte[] Mark;

        public override int Size => base.Size + BookId.Size + FixedSerial.Size + sizeof(BookSectionType) + sizeof(BookEncodingType) + SectionHash.Size + Data.GetVarSize() + sizeof(byte) + Mark.GetVarSize();
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
        public BookSectionTransaction()
          : base(TransactionType.BookSectionTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Data = new byte[] { 0x00 };
            this.Mark = new byte[] { 0x00 };
        }
        public BookSectionTransaction(UInt256 bookId)
            : this()
        {
            BookId = bookId;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            var bookState = Blockchain.Singleton.Store.GetBookState(this.BookId);
            yield return bookState.CopyrightOwner;
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (this.BookId.IsNull()) return false;
            if (this.Data.IsNullOrEmpty()) return false;
            if (this.Mark.IsNullOrEmpty()) return false;
            if (this.SectionHash.IsNull()) return false;
            var bookState = Blockchain.Singleton.Store.GetBookState(this.BookId);
            if (bookState.IsNull()) return false;
            if (bookState.Book.BookStorageType == BookStorageType.OnChain && !this.SectionHash.Equals(this.DataHash)) return false;
            return base.Verify(snapshot, mempool);
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            BookId = reader.ReadSerializable<UInt256>();
            FixedSerial = reader.ReadSerializable<Fixed8>();
            BookSectionType = (BookSectionType)reader.ReadByte();
            BookEncodingType = (BookEncodingType)reader.ReadByte();
            SectionHash = reader.ReadSerializable<UInt256>();
            Data = reader.ReadVarBytes();
            Flag = reader.ReadByte();
            Mark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(BookId);
            writer.Write(FixedSerial);
            writer.Write((byte)BookSectionType);
            writer.Write((byte)BookEncodingType);
            writer.Write(SectionHash);
            writer.WriteVarBytes(Data);
            writer.Write(Flag);
            writer.WriteVarBytes(Mark);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["bookid"] = BookId.ToString();
            json["fixedserial"] = FixedSerial.ToString();
            json["booksectiontype"] = BookSectionType.Value();
            json["bookencodingtype"] = BookEncodingType.Value();
            json["data"] = Data.ToHexString();
            json["flag"] = Flag.ToString();
            json["mark"] = Mark.ToHexString();
            return json;
        }
    }
    public class BookTransferTransaction : Transaction
    {
        public UInt160 Owner;
        public CopyrightOwner NewCopyrightOwner;
        public SignatureValidator<BookCopyrightAuthentication> BookCopyrightAuthentication;
        public byte[] Data;
        public override int Size => base.Size + Owner.Size + NewCopyrightOwner.Size + BookCopyrightAuthentication.Size + Data.GetVarSize();

        public BookTransferTransaction()
          : base(TransactionType.BookTransferTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Data = new byte[] { 0x00 };
        }

        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return Owner;
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (NewCopyrightOwner.IsNull()) return false;
            if (BookCopyrightAuthentication.IsNull()) return false;
            if (!BookCopyrightAuthentication.Verify()) return false;
            if (BookCopyrightAuthentication.Target.Amount <= Fixed8.Zero && BookCopyrightAuthentication.Target.NewOwner == UInt160.Zero) return false;
            var bookState = Blockchain.Singleton.Store.GetBookState(this.BookCopyrightAuthentication.Target.BookId);
            if (bookState.IsNull()) return false;
            var oldOwner = Contract.CreateSignatureRedeemScript(this.BookCopyrightAuthentication.Target.PublicKey).ToScriptHash();
            if (!bookState.CopyrightOwner.Equals(oldOwner)) return false;
            if (this.BookCopyrightAuthentication.Target.NewOwner != UInt160.Zero && this.BookCopyrightAuthentication.Target.NewOwner != Owner) return false;
            if (this.BookCopyrightAuthentication.Target.Amount > Fixed8.Zero)
            {
                if (this.Outputs.IsNullOrEmpty()) return false;
                var outputs = this.Outputs.Where(m => m.AssetId.Equals(Blockchain.OXC) && m.ScriptHash.Equals(oldOwner));
                if (outputs.IsNullOrEmpty()) return false;
                if (outputs.Sum(m => m.Value) < BookCopyrightAuthentication.Target.Amount) return false;
                if (this.BookCopyrightAuthentication.Target.MaxIndex > 0)
                {
                    if (this.BookCopyrightAuthentication.Target.MaxIndex <= snapshot.Height) return false;
                }
                if (this.BookCopyrightAuthentication.Target.MinIndex > 0)
                {
                    if (this.BookCopyrightAuthentication.Target.MinIndex > snapshot.Height + 1) return false;
                }
            }
            return base.Verify(snapshot, mempool);
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Owner = reader.ReadSerializable<UInt160>();
            NewCopyrightOwner = reader.ReadSerializable<CopyrightOwner>();
            BookCopyrightAuthentication = reader.ReadSerializable<SignatureValidator<BookCopyrightAuthentication>>();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Owner);
            writer.Write(NewCopyrightOwner);
            writer.Write(BookCopyrightAuthentication);
            writer.WriteVarBytes(Data);
        }
    }
}
