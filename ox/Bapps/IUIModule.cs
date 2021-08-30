using OX.Network.P2P.Payloads;
using OX.IO.Json;

namespace OX.Bapps
{
    public interface IUIModule
    {
        Bapp Bapp { get; set; }
        string ModuleName { get; }
        JObject moduleWalletSection { get; }
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
        void OnBlock(Block block);
        void LoadBappModuleWalletSection(JObject moduleSectionObject);
    }
}
