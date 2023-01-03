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
    public class BookTransaction : Transaction
    {
        public ECPoint Author;
        public BookType BookType;
        public BookStorageType BookStorageType;
        public byte[] Data;
        public byte[] Mark;

        public override int Size => base.Size + Author.Size + sizeof(BookType) + sizeof(BookStorageType) + Data.GetVarSize() + Mark.GetVarSize();

        public BookTransaction()
          : base(TransactionType.BookTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.Data = new byte[] { 0x00 };
            this.Mark = new byte[] { 0x00 };
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
            if (this.Mark.IsNullOrEmpty()) return false;
            return base.Verify(snapshot, mempool);
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Author = reader.ReadSerializable<ECPoint>();
            BookType = (BookType)reader.ReadByte();
            BookStorageType = (BookStorageType)reader.ReadByte();
            Data = reader.ReadVarBytes();
            Mark = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Author);
            writer.Write((byte)BookType);
            writer.Write((byte)BookStorageType);
            writer.WriteVarBytes(Data);
            writer.WriteVarBytes(Mark);
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
            yield return Contract.CreateSignatureRedeemScript(bookState.Book.Author).ToScriptHash();
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
}
