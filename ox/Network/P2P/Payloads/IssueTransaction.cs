﻿using OX.IO;
using OX.Ledger;
using OX.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OX.Network.P2P.Payloads
{
    public class IssueTransaction : Transaction
    {
        public override Fixed8 SystemFee
        {
            get
            {
                //if (Version >= 1) return Fixed8.Zero;
                if (!NeedOutputFee)
                    return AttributesFee;
                return AttributesFee + OutputFee;
            }
        }
        public Fixed8 AttributesFee => Fixed8.One * this.Attributes.Where(m => m.Usage >= TransactionAttributeUsage.Remark && m.Usage < TransactionAttributeUsage.EthSignature && m.Data.GetVarSize() > 8).Count();
        public override bool NeedOutputFee => !(Outputs.All(p => p.AssetId == Blockchain.OXS_Token.Hash || p.AssetId == Blockchain.OXC_Token.Hash));
        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
        }

        public override UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying(snapshot));
            foreach (TransactionResult result in GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
            {
                AssetState asset = snapshot.Assets.TryGet(result.AssetId);
                if (asset == null) throw new InvalidOperationException();
                hashes.Add(asset.Issuer);
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(snapshot, mempool)) return false;
            TransactionResult[] results = GetTransactionResults()?.Where(p => p.Amount < Fixed8.Zero).ToArray();
            if (results == null) return false;
            foreach (TransactionResult r in results)
            {
                AssetState asset = snapshot.Assets.TryGet(r.AssetId);
                if (asset == null) return false;
                if (asset.Amount < Fixed8.Zero) continue;
                Fixed8 quantity_issued = asset.Available + mempool.OfType<IssueTransaction>().Where(p => p != this).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
                if (asset.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
