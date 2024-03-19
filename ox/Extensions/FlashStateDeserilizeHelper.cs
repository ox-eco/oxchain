using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OX.IO;
using OX.Network.P2P.Payloads;

namespace OX
{
    public static class FlashStateDeserilizeHelper
    {
        public static FlashState DeserilizeFlashState(this byte[] flashData, byte flashStateType)
        {
            try
            {
                FlashStateType tt = (FlashStateType)flashStateType;
                switch (tt)
                {
                    case FlashStateType.FlashLog:
                        return flashData.AsSerializable<FlashLog>();
                    case FlashStateType.FlashMulticast:
                        return flashData.AsSerializable<FlashMulticast>();
                    case FlashStateType.FlashUnicast:
                        return flashData.AsSerializable<FlashUnicast>();
                    case FlashStateType.FlashMulticastNotice:
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
