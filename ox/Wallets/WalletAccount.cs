using OX.SmartContract;

namespace OX.Wallets
{
    public interface IWalletAccountProfile
    {

    }
    public abstract class WalletAccount
    {
        public readonly UInt160 ScriptHash;
        public string Label;
        public string Group;
        public string AccessCode;
        public bool IsDefault;
        public IWalletAccountProfile Profile;
        public bool Lock;
        public Contract Contract;

        public string Address => ScriptHash.ToAddress();
        public abstract bool HasKey { get; }
        public bool WatchOnly => Contract == null;

        public abstract KeyPair GetKey();

        protected WalletAccount(UInt160 scriptHash)
        {
            this.ScriptHash = scriptHash;
        }
    }
}
