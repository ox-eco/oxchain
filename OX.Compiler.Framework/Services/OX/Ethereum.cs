using System.Text;

namespace OX.SmartContract.Framework.Services
{
    public static class Ethereum
    {
        [Syscall("OX.Ethereum.EcRecover")]
        public static extern string EcRecover(byte[] message, byte[] signature);
        [Syscall("OX.Ethereum.EcRecoverString")]
        public static extern string EcRecoverString(byte[] message, byte[] signature);
    }
}
