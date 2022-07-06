namespace OX.SmartContract.Framework.Services
{
    public class Asset
    {
        public extern byte[] AssetId
        {
            [Syscall("OX.Asset.GetAssetId")]
            get;
        }

        public extern byte AssetType
        {
            [Syscall("OX.Asset.GetAssetType")]
            get;
        }

        public extern long Amount
        {
            [Syscall("OX.Asset.GetAmount")]
            get;
        }

        public extern long Available
        {
            [Syscall("OX.Asset.GetAvailable")]
            get;
        }

        public extern byte Precision
        {
            [Syscall("OX.Asset.GetPrecision")]
            get;
        }

        public extern byte[] Owner
        {
            [Syscall("OX.Asset.GetOwner")]
            get;
        }

        public extern byte[] Admin
        {
            [Syscall("OX.Asset.GetAdmin")]
            get;
        }

        public extern byte[] Issuer
        {
            [Syscall("OX.Asset.GetIssuer")]
            get;
        }

        [Syscall("OX.Asset.Create")]
        public static extern Asset Create(byte asset_type, string name, long amount, byte precision, byte[] owner, byte[] admin, byte[] issuer);

        [Syscall("OX.Asset.Renew")]
        public extern uint Renew(byte years);
    }
}
