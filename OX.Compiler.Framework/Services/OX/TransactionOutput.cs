namespace OX.SmartContract.Framework.Services
{
    public class TransactionOutput : IApiInterface
    {
        public extern byte[] AssetId
        {
            [Syscall("OX.Output.GetAssetId")]
            get;
        }

        public extern long Value
        {
            [Syscall("OX.Output.GetValue")]
            get;
        }

        public extern byte[] ScriptHash
        {
            [Syscall("OX.Output.GetScriptHash")]
            get;
        }
    }
}
