using OX.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OX.Bapps
{
    public class BappEvent
    {
    }
    public class CrossBappMessage
    {
        public int MessageType { get; set; }
        public string Content { get; set; }
        public byte[] MessageData { get; set; }
        public Bapp From { get; set; }
        public Bapp To { get; set; }
    }
}
