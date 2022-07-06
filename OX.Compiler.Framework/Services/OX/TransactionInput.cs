namespace OX.SmartContract.Framework.Services
{
    public class TransactionInput : IApiInterface
    {
        public extern byte[] PrevHash
        {
            [Syscall("OX.Input.GetHash")]
            get;
        }

        public extern ushort PrevIndex
        {
            [Syscall("OX.Input.GetIndex")]
            get;
        }
    }
}
