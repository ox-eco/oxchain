using OX.Network.P2P.Payloads;

namespace OX.Bapps
{
    public interface IBappApi : IBappPort
    {
        void OnBlock(Block block);
        void BeforeOnBlock(Block block);
        void AfterOnBlock(Block block);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
    }


}
