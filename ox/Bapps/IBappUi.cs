using OX.Network.P2P.Payloads;
using OX.Wallets;

namespace OX.Bapps
{
    public interface IBappUi : IBappPort
    {
        IUIModule[] Modules { get; }
        void OnFlashState(FlashState flashState);
        void OnBlock(Block block);
        void BeforeOnBlock(Block block);
        void AfterOnBlock(Block block);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
        void OnRebuild();
    }

}
