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
        bool InputFilter(FlashState fs);
        bool OutputFilter(FlashState fs);
    }
    public interface IFlashStateCommentFilter
    {
        bool InputFilter(FlashStateComment fsc);
        bool OutputFilter(FlashStateComment fsc);
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
