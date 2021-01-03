using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.Network.P2P.Payloads;

namespace OX
{
    public static class TransactionDeserilizeHelper
    {
        public static Transaction DeserilizeTransaction(this byte[] TxData, byte TxType)
        {
            try
            {
                TransactionType tt = (TransactionType)TxType;
                switch (tt)
                {
                    case TransactionType.MinerTransaction:
                        return TxData.AsSerializable<MinerTransaction>();
                    case TransactionType.IssueTransaction:
                        return TxData.AsSerializable<IssueTransaction>();
                    case TransactionType.ClaimTransaction:
                        return TxData.AsSerializable<ClaimTransaction>();
                    //case TransactionType.EnrollmentTransaction:
                    //    return TxData.AsSerializable<EnrollmentTransaction>();
                    //case TransactionType.RegisterTransaction:
                    //    return TxData.AsSerializable<RegisterTransaction>();
                    case TransactionType.ContractTransaction:
                        return TxData.AsSerializable<ContractTransaction>();
                    case TransactionType.StateTransaction:
                        return TxData.AsSerializable<StateTransaction>();
                    //case TransactionType.PublishTransaction:
                    //    return TxData.AsSerializable<PublishTransaction>();
                    case TransactionType.InvocationTransaction:
                        return TxData.AsSerializable<InvocationTransaction>();
                    case TransactionType.BillTransaction:
                        return TxData.AsSerializable<BillTransaction>();
                    case TransactionType.CharitableTransaction:
                        return TxData.AsSerializable<CharitableTransaction>();
                    case TransactionType.GovementTransaction:
                        return TxData.AsSerializable<GovementTransaction>();
                    case TransactionType.DetainTransaction:
                        return TxData.AsSerializable<DetainTransaction>();
                    case TransactionType.EventTransaction:
                        return TxData.AsSerializable<EventTransaction>();
                    case TransactionType.ReplyTransaction:
                        return TxData.AsSerializable<ReplyTransaction>();
                    case TransactionType.AskTransaction:
                        return TxData.AsSerializable<AskTransaction>();
                    case TransactionType.TreatyTransaction:
                        return TxData.AsSerializable<TreatyTransaction>();
                }
                return default;

            }
            catch
            {
                return default;
            }
        }
    }
}
