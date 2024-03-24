using OX.Network.P2P.Payloads;
using System.Collections.Generic;

namespace OX.Bapps
{
    public interface IBappApi : IBappPort
    {
        void OnFlashMessage(FlashMessage flashMessage);
        void OnBlock(Block block);
        void BeforeOnBlock(Block block);
        void AfterOnBlock(Block block);
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
        bool ProcessAsync(Microsoft.AspNetCore.Http.HttpContext context, string path, Dictionary<string, string> query, out string resp);
        bool GetHomeHtml(Microsoft.AspNetCore.Http.HttpContext context, string path, Dictionary<string, string> query, out string resp);
    }
}
