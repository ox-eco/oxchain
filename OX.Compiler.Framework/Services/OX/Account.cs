namespace OX.SmartContract.Framework.Services
{
    public class Account
    {
        public extern byte[] ScriptHash
        {
            [Syscall("OX.Account.GetScriptHash")]
            get;
        }

        public extern byte[][] Votes
        {
            [Syscall("OX.Account.GetVotes")]
            get;
        }

        [Syscall("OX.Account.GetBalance")]
        public extern long GetBalance(byte[] asset_id);

        [Syscall("OX.Account.IsStandard")]
        public static extern bool IsStandard(byte[] scripthash);
    }
}
