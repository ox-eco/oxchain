using OX.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OX.Persistence
{
    public interface IFlashStateFilter
    {
        bool Filter(FlashState flashState);
    }
}
