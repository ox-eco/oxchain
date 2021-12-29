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
    public enum RewardAmount : byte
    {
        _10000 = 0x01,
        _100000 = 0x02,
        _1000000 = 0x04,
        _10000000 = 0x08
    }
    public class RewardTransaction : Transaction
    {
        public RewardAmount RewardAmount;
        public byte[] Data;

        public override int Size => base.Size + sizeof(RewardAmount) + Data.GetVarSize();
        public override Fixed8 SystemFee
        {
            get
            {
                switch (RewardAmount)
                {
                    case RewardAmount._10000:
                        return Fixed8.One * 10000;
                    case RewardAmount._100000:
                        return Fixed8.One * 100000;
                    case RewardAmount._1000000:
                        return Fixed8.One * 1000000;
                    case RewardAmount._10000000:
                        return Fixed8.One * 10000000;
                    default:
                        return Fixed8.One * 10000;
                }
            }
        }
        public RewardTransaction()
          : base(TransactionType.RewardTransaction)
        {
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            Data = new byte[] { 0x00 };
        }


        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            RewardAmount = (RewardAmount)reader.ReadByte();
            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write((byte)RewardAmount);
            writer.WriteVarBytes(Data);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (SystemFee < Fixed8.OXT) return false;
            return base.Verify(snapshot, mempool);
        }
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
