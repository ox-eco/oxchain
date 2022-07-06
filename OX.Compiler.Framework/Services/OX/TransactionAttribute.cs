namespace OX.SmartContract.Framework.Services
{
    public class TransactionAttribute : IApiInterface
    {
        public extern byte Usage
        {
            [Syscall("OX.Attribute.GetUsage")]
            get;
        }

        public extern byte[] Data
        {
            [Syscall("OX.Attribute.GetData")]
            get;
        }
    }
}
