namespace OX.SmartContract.Framework.Services
{
    public class Transaction : IScriptContainer
    {
        public extern byte[] Hash
        {
            [Syscall("OX.Transaction.GetHash")]
            get;
        }
        public extern byte[] InputHash
        {
            [Syscall("OX.Transaction.GetInputHash")]
            get;
        }
        public extern byte[] OutputHash
        {
            [Syscall("OX.Transaction.GetOutputHash")]
            get;
        }
        public extern byte[] InputOutputHash
        {
            [Syscall("OX.Transaction.GetInputOutputHash")]
            get;
        }
        public extern byte Type
        {
            [Syscall("OX.Transaction.GetType")]
            get;
        }

        [Syscall("OX.Transaction.GetAttributes")]
        public extern TransactionAttribute[] GetAttributes();

        [Syscall("OX.Transaction.GetInputs")]
        public extern TransactionInput[] GetInputs();

        [Syscall("OX.Transaction.GetOutputs")]
        public extern TransactionOutput[] GetOutputs();

        [Syscall("OX.Transaction.GetReferences")]
        public extern TransactionOutput[] GetReferences();

        [Syscall("OX.Transaction.GetUnspentCoins")]
        public extern TransactionOutput[] GetUnspentCoins();
    }
}
