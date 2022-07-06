namespace OX.SmartContract.Framework.Services
{
    public static class Blockchain
    {
        [Syscall("OX.Blockchain.GetHeight")]
        public static extern uint GetHeight();

        [Syscall("OX.Blockchain.GetHeader")]
        public static extern Header GetHeader(uint height);

        [Syscall("OX.Blockchain.GetHeader")]
        public static extern Header GetHeader(byte[] hash);

        [Syscall("OX.Blockchain.GetBlock")]
        public static extern Block GetBlock(uint height);

        [Syscall("OX.Blockchain.GetBlock")]
        public static extern Block GetBlock(byte[] hash);

        [Syscall("OX.Blockchain.GetTransaction")]
        public static extern Transaction GetTransaction(byte[] hash);

        [Syscall("OX.Blockchain.GetAccount")]
        public static extern Account GetAccount(byte[] script_hash);

        [Syscall("OX.Blockchain.GetValidators")]
        public static extern byte[][] GetValidators();

        [Syscall("OX.Blockchain.GetAsset")]
        public static extern Asset GetAsset(byte[] asset_id);

        [Syscall("OX.Blockchain.GetContract")]
        public static extern Contract GetContract(byte[] script_hash);
    }
}
