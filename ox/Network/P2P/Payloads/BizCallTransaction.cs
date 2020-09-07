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
using System.Data;

namespace OX.Network.P2P.Payloads
{
    public class BizCallTransaction : EdgeTransaction
    {
        public UInt160 From;
        public byte CallType;
        public byte[] Data;
        public uint MaxIndex;
        public override int Size => base.Size + From.Size + sizeof(byte) + Data.GetVarSize() + sizeof(uint);
        public BizCallTransaction()
             : base(TransactionType.BizCallTransaction)
        {
            this.BizTxState = BizTransactionStatus.OnChain;
            this.Inputs = new CoinReference[0];
            this.Outputs = new TransactionOutput[0];
            this.Attributes = new TransactionAttribute[0];
            //this.Witnesses = new Witness[0];
        }
        protected override void DeserializeEdgeData(BinaryReader reader)
        {
            From = reader.ReadSerializable<UInt160>();
            CallType = reader.ReadByte();
            Data = reader.ReadVarBytes();
            MaxIndex = reader.ReadUInt32();
        }
        protected override void SerializeEdgeData(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(CallType);
            writer.WriteVarBytes(Data);
            writer.Write(MaxIndex);
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (MaxIndex > 0 && MaxIndex <= snapshot.Height) return false;
            if (!base.Verify(snapshot, mempool)) return false;
            return Blockchain.Singleton.VerifyBizValidator(this.BizScriptHash,out Fixed8 balance);
        }
    }
}
