using System;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public class GovementTransaction : Transaction
    {
        public GovementTransaction()
            : base(TransactionType.GovementTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
        }
    }
}
