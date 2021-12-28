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
    public class RewardTransaction : Transaction
    {
        public byte[] Data;

        public override int Size => base.Size + Data.GetVarSize();

        public RewardTransaction(Fixed8 rewardFee)
          : base(TransactionType.RewardTransaction)
        {
            this.RewardSystemFee = rewardFee;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            Data = new byte[] { 0x00 };
        }


        protected override void DeserializeExclusiveData(BinaryReader reader)
        {

            Data = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {

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
