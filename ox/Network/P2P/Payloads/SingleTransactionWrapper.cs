using OX.Wallets;
using System.Collections.Generic;

namespace OX.Network.P2P.Payloads
{
    public class SingleTransactionWrapper<T> where T : Transaction, new()
    {
        T tx;
        public UInt160 From { get; private set; }
        public SingleTransactionWrapper(UInt160 from, TransactionOutput output = default) : base()
        {
            this.From = from;
            tx = new T();
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            tx.Attributes = attributes.ToArray();
            if (output.IsNotNull())
                tx.Outputs = new TransactionOutput[] { output };
        }
        public SingleTransactionWrapper(UInt160 from, TransactionOutput[] outputs = default) : base()
        {
            this.From = from;
            tx = new T();
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            tx.Attributes = attributes.ToArray();
            if (outputs != default)
                tx.Outputs = outputs;
        }
        public SingleTransactionWrapper(UInt160 from, UInt160 to, UInt256 assetId, Fixed8 amount) : this(from, new TransactionOutput() { AssetId = assetId, ScriptHash = to, Value = amount })
        {
        }
        public T Get()
        {
            return tx;
        }
    }
    public class SingleAskTransactionWrapper : SingleTransactionWrapper<AskTransaction>
    {
        public WalletAccount Account { get; private set; }
        public SingleAskTransactionWrapper(WalletAccount account, TransactionOutput output = default) : base(account.ScriptHash, output)
        {
            this.Account = account;
        }
        public SingleAskTransactionWrapper(WalletAccount account, TransactionOutput[] outputs = default) : base(account.ScriptHash, outputs)
        {
            this.Account = account;
        }
    }
}
