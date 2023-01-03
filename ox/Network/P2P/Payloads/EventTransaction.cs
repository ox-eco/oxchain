using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using OX.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class EventTransaction : Transaction
    {
        public UInt160 ScriptHash;
        public EventType EventType;
        public uint DataFormat;
        public byte[] Data;

        public override int Size => base.Size + ScriptHash.Size + sizeof(EventType) + sizeof(uint) + Data.GetVarSize();
        public override Fixed8 SystemFee
        {
            get
            {
                switch (EventType)
                {
                    case EventType.Board:
                        return Fixed8.One * 100;
                    case EventType.Engrave:
                        return Fixed8.One;
                    default:
                        return Fixed8.One;
                }
            }
        }
        public EventTransaction()
          : base(TransactionType.EventTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            this.DataFormat = 0;
            Data = new byte[] { 0x00 };
        }
        public EventTransaction(UInt160 scriptHash)
            : this()
        {
            ScriptHash = scriptHash;
        }
        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            hashes.UnionWith(GetScriptHashesForVerifying_Validator());
            return hashes.OrderBy(p => p).ToArray();
        }
        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator()
        {
            yield return this.ScriptHash;
        }
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            ScriptHash = reader.ReadSerializable<UInt160>();
            EventType = (EventType)reader.ReadByte();
            DataFormat = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(ScriptHash);
            writer.Write((byte)EventType);
            writer.Write(DataFormat);
            writer.WriteVarBytes(Data);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["scripthash"] = ScriptHash.ToAddress();
            json["eventtype"] = EventType.Value();
            json["dataformat"] = DataFormat.ToString();
            json["data"] = Data.ToHexString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            bool ok = false;
            switch (EventType)
            {
                case EventType.Board:
                    ok = VerifyBoard();
                    break;
                case EventType.Engrave:
                    ok = VerifyEngrave();
                    break;
                case EventType.Digg:
                    ok = VerifyDigg();
                    break;
            }
            if (!ok) return false;
            return base.Verify(snapshot, mempool);
        }
        bool VerifyBoard()
        {
            try
            {
                var board = Data.AsSerializable<Board>();
                if (board.IsNull()) return false;
                if (board.Name.GetVarSize() > 64) return false;
                if (board.Remark.GetVarSize() > 1024) return false;
                if (board.Data.GetVarSize() > 1024 * 50) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        bool VerifyEngrave()
        {
            try
            {
                var engrave = Data.AsSerializable<Engrave>();
                if (engrave.IsNull()) return false;
                if (engrave.Title.IsNullOrEmpty()) return false;
                if (engrave.Title.GetVarSize() > 64) return false;
                if (engrave.Message.IsNullOrEmpty()) return false;
                if (engrave.Message.GetVarSize() > 1024 * 10) return false;
                if (engrave.Data.GetVarSize() > 1024) return false;
                var sh = Blockchain.Singleton.GetBlockHash(engrave.BoardTxIndex);
                if (sh.IsNull()) return false;
                var block = Blockchain.Singleton.GetBlock(sh);
                if (block.IsNull()) return false;
                if (block.Transactions.Length <= engrave.BoardTxPosition) return false;
                if (block.Transactions[engrave.BoardTxPosition] is EventTransaction et)
                    if (et.EventType == EventType.Board)
                    {
                        var board = et.Data.AsSerializable<Board>();
                        if (board.IsNull()) return false;
                        return board.IsOpen || this.ScriptHash.Equals(et.ScriptHash);
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }
        bool VerifyDigg()
        {
            try
            {
                var digg = Data.AsSerializable<Digg>();
                if (digg.IsNull()) return false;
                if (digg.Message.IsNullOrEmpty()) return false;
                if (digg.Message.GetVarSize() > 256) return false;
                if (this.Outputs.IsNullOrEmpty()) return false;
                Transaction tx = default;
                using (var snapshot = Blockchain.Singleton.GetSnapshot())
                {
                    tx = snapshot.GetTransaction(digg.EngraveId);
                }
                if (tx.IsNull()) return false;
                if (tx is EventTransaction et)
                {
                    if (!this.ScriptHash.Equals(et.ScriptHash))
                    {
                        var pts = this.Outputs.Where(m => m.ScriptHash == et.ScriptHash);
                        if (pts.IsNullOrEmpty()) return false;
                        if (pts.Sum(m => m.Value) < Fixed8.One) return false;
                    }
                    if (et.EventType != EventType.Engrave)
                        return false;
                    var engrave = et.Data.AsSerializable<Engrave>();
                    if (engrave.IsNull()) return false;
                    if (!engrave.IsOpen) return false;
                }
                else
                    return false;

                if (digg.AtDiggId != null)
                {
                    if (!digg.AtDiggId.Equals(UInt256.Zero))
                    {
                        using (var snapshot = Blockchain.Singleton.GetSnapshot())
                        {
                            tx = snapshot.GetTransaction(digg.AtDiggId);
                        }
                        if (tx.IsNull()) return false;
                        if (tx is EventTransaction tt)
                        {
                            if (tt.EventType != EventType.Digg)
                                return false;
                        }
                        else
                            return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
