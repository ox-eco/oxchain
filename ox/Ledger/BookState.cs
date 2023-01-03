using OX.IO;
using OX.IO.Json;
using OX.Network.P2P.Payloads;
using OX.Cryptography;
using OX.Network.P2P;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace OX.Ledger
{

    public class BookState : StateBase, ICloneable<BookState>
    {
        public BookTransaction Book;
        public uint BlockIndex;
        public ushort N;
        public UInt256 DataHash;
        public Dictionary<Fixed8, UInt256> Sections;

        public override int Size => base.Size + Book.Size + sizeof(uint) + sizeof(ushort) + DataHash.Size + IO.Helper.GetVarSize(Sections.Count) + Sections.Count * (8 + 32);
        public BookState()
        {
            this.Sections = new Dictionary<Fixed8, UInt256>();
        }
        BookState ICloneable<BookState>.Clone()
        {
            return new BookState
            {
                Book = Book,
                BlockIndex = BlockIndex,
                N = N,
                DataHash = DataHash,
                Sections = Sections.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            var tx = Transaction.DeserializeFrom(reader);
            if (tx.IsNotNull() && tx is BookTransaction book)
                this.Book = book;
            this.BlockIndex = reader.ReadUInt32();
            this.N = reader.ReadUInt16();
            this.DataHash = reader.ReadSerializable<UInt256>();
            int count = (int)reader.ReadVarInt();
            Sections = new Dictionary<Fixed8, UInt256>(count);
            for (int i = 0; i < count; i++)
            {
                Fixed8 key = reader.ReadSerializable<Fixed8>();
                UInt256 value = reader.ReadSerializable<UInt256>();
                Sections.Add(key, value);
            }
        }

        void ICloneable<BookState>.FromReplica(BookState replica)
        {
            this.Book = replica.Book;
            this.BlockIndex = replica.BlockIndex;
            this.N = replica.N;
            this.DataHash = replica.DataHash;
            this.Sections = replica.Sections;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Book);
            writer.Write(BlockIndex);
            writer.Write(N);
            writer.Write(DataHash);
            writer.WriteVarInt(Sections.Count);
            foreach (var pair in Sections)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            if (Book.IsNotNull())
            {
                json["book"] = Book.ToJson();
            }
            json["blockindex"] = BlockIndex.ToString();
            json["n"] = N.ToString();
            json["datahash"] = DataHash.ToString();
            json["sections"] = Sections.Select(p =>
            {
                JObject balance = new JObject();
                balance["fixedserial"] = p.Key.ToString();
                balance["sectionid"] = p.Value.ToString();
                return balance;
            }).ToArray();
            return json;
        }
    }

}
