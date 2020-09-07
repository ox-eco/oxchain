using System;
using System.IO;
using System.Collections.Generic;
using OX.Ledger;
using OX.Persistence;

namespace OX.Network.P2P.Payloads
{
    public class ContractTransaction : Transaction
    {
        public ContractTransaction()
            : base(TransactionType.ContractTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
        }
        public override bool Verify(Snapshot snapshot, IEnumerable<Transaction> mempool)
        {
            foreach (var witness in this.Witnesses)
            {
                var acts = snapshot.Accounts.GetAndChange(witness.ScriptHash, () => null);
                if (acts.IsNotNull())
                {
                    if (acts.DetainState != DetainStatus.UnFreeze)
                        return false;
                }
            }
            return base.Verify(snapshot, mempool);
        }
    }
}
