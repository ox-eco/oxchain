using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.Network.P2P.Payloads;

namespace OX.Bapps
{
    public interface IUIModule
    {
        Bapp Bapp { get; set; }
        string ModuleName { get; }
        void OnBappEvent(BappEvent bappEvent);
        void OnCrossBappMessage(CrossBappMessage message);
        void OnBlock(Block block);
    }
}
