﻿using OX.IO;
using OX.IO.Json;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;
        public Fixed8 Gas;

        public override int Size => base.Size  // Transaction base size
           + Script.GetVarSize()               // Script variable size
           + (Version >= 1 ? Gas.Size : 0);    // Gas Fixed8 size (for version >= 1)

        public override Fixed8 SystemFee => Gas;

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
            Script = reader.ReadVarBytes(65536);
            if (Script.Length == 0) throw new FormatException();
            if (Version >= 1)
            {
                Gas = reader.ReadSerializable<Fixed8>();
                if (Gas < Fixed8.Zero) throw new FormatException();
            }
            else
            {
                Gas = Fixed8.Zero;
            }
        }

        public static Fixed8 GetGas(Fixed8 consumed)
        {
            Fixed8 gas = consumed - Fixed8.FromDecimal(10);
            if (gas <= Fixed8.Zero) return Fixed8.Zero;
            return gas.Ceiling();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            if (Version >= 1)
                writer.Write(Gas);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            return json;
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (Gas.GetData() % 100000000 != 0) return false;
            return base.Verify(snapshot, mempool);
        }
    }
}
