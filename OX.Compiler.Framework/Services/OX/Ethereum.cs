using System.Text;

namespace OX.SmartContract.Framework.Services
{
    public static class Ethereum
    {
        [Syscall("OX.Ethereum.EncodeUTF8AndEcRecover")]
        public static extern string EncodeUTF8AndEcRecover(string message, string signature);
    }
}
