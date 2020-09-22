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
        public BappEventItem[] EventItems { get; set; }
        public bool ContainEventType<EventType>(EventType eventType, out BappEventItem[] eventItems) where EventType : struct
        {
            eventItems = default;
            if (this.EventItems.IsNullOrEmpty()) return false;
            eventItems = this.EventItems.Where(m => m.EventType == eventType.Value())?.ToArray();
            return eventItems.IsNotNullAndEmpty();
        }
    }
    public class BappEventItem
    {
        public int EventType { get; set; }
        public Object Arg { get; set; }
    }
    public class CrossBappMessage
    {
        public CrossBappMessage()
        {
            MessageType = 0;
        }
        public int MessageType { get; set; }
        public string Content { get; set; }
        public byte[] MessageData { get; set; }
        public Bapp From { get; set; }
        public Bapp To { get; set; }
    }
}
