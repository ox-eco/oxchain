using OX.IO;
using OX.IO.Json;
using OX.Ledger;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OX.Cryptography;
using OX.Cryptography.ECC;
using OX.SmartContract;

namespace OX.Network.P2P.Payloads
{
    public class BillTransaction : CenterTransaction
    {
        public uint Nonce;

        public Record[] Records;

        public override Fixed8 NetworkFee => Fixed8.Zero;
        public override int Size => base.Size + sizeof(uint) + Records.GetVarSize();

        public BillTransaction()
            : base(TransactionType.BillTransaction)
        {
            this.BizTxState = BizTransactionStatus.OnChain;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            //this.Witnesses = new Witness[0];
        }

        protected override void DeserializeCenterData(BinaryReader reader)
        {
            Nonce = reader.ReadUInt32();
            Records = reader.ReadSerializableArray<Record>();
        }
        protected override void SerializeCenterData(BinaryWriter writer)
        {
            writer.Write(Nonce);
            writer.Write(Records);
        }


    }
}
