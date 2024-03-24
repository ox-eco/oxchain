using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.Network.P2P.Payloads;

namespace OX
{
    public static class FlashMessageDeserilizeHelper
    {
        public static FlashMessage DeserilizeFlashMessage(this byte[] flashData, byte flashMessageType)
        {
            try
            {
                FlashMessageType tt = (FlashMessageType)flashMessageType;
                switch (tt)
                {
                    case FlashMessageType.FlashState:
                        return flashData.AsSerializable<FlashState>();
                    case FlashMessageType.FlashMulticast:
                        return flashData.AsSerializable<FlashMulticast>();
                    case FlashMessageType.FlashUnicast:
                        return flashData.AsSerializable<FlashUnicast>();
                    case FlashMessageType.FlashMulticastNotice:
                        return flashData.AsSerializable<FlashMulticastNotice>();
                }
                return default;

            }
            catch
            {
                return default;
            }
        }
    }
}
