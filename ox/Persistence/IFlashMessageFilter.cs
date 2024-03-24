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
        bool InputFilter(FlashState fl);
        bool OutputFilter(FlashState fl);
    }
    public interface IFlashMulticastFilter
    {
        bool Filter(FlashMulticast fm);
    }
    public interface IFlashUnicastFilter
    {
        bool Filter(FlashUnicast fu);
    }
}
