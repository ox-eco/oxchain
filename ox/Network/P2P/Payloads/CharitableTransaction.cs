using System;
using System.IO;

namespace OX.Network.P2P.Payloads
{
    public class CharitableTransaction : Transaction
    {
        public CharitableTransaction()
            : base(TransactionType.CharitableTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
        }
    }
}
