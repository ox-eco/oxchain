using OX.Network.P2P.Payloads;
using OX.Wallets;

namespace OX.Bapps
{
    public interface IBappProvider : IBappPort
    {
        Wallet Wallet { get; set; }
        void OnBlock(Block block);
        void OnRebuild(Wallet wallet = null);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
    }

}
