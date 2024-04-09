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
        bool StateInputFilter(FlashState fs);
        bool StateOutputFilter(FlashState fs);
        bool CommentInputFilter(FlashStateComment fsc);
        bool CommentOutputFilter(FlashStateComment fsc);
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
