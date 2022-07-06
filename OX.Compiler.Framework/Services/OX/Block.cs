namespace OX.SmartContract.Framework.Services
{
    public class Block : Header
    {
        [Syscall("OX.Block.GetTransactionCount")]
        public extern int GetTransactionCount();

        [Syscall("OX.Block.GetTransactions")]
        public extern Transaction[] GetTransactions();

        [Syscall("OX.Block.GetTransaction")]
        public extern Transaction GetTransaction(int index);
    }
}
