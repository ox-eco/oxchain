namespace OX.SmartContract.Framework.Services
{
    public class InvocationTransaction : Transaction
    {
        public extern byte[] Script
        {
            [Syscall("OX.InvocationTransaction.GetScript")]
            get;
        }
    }
}
