using OX.Network.P2P.Payloads;

namespace OX.Bapps
{
    public interface IBappUi : IBappPort
    {
        IUIModule[] Modules { get; }
        void OnBlock(Block block);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
    }

}
