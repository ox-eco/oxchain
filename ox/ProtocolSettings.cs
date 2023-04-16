using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Net;

namespace OX
{

    public class ProtocolSettings
    {
        public uint Magic { get; }
        public byte AddressVersion { get; }
        public string[] StandbyValidators { get; }
        public string[] SeedList { get; internal set; }
        public bool OnlySeed { get; internal set; }
        public IReadOnlyDictionary<TransactionType, Fixed8> SystemFee { get; }
        public Fixed8 LowPriorityThreshold { get; }
        public uint SecondsPerBlock { get; }
        public Fixed8 BappDetainOXS { get; }
        static ProtocolSettings _default;

        public static ProtocolSettings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new ProtocolSettings();
                }

                return _default;
            }
        }

        private ProtocolSettings()
        {
            this.Magic = 10141116;
            this.AddressVersion = 23;
            this.StandbyValidators = new[]
            {
                "0377e20c26273f482c10ca991ec1c077fa1dd7a47e35e1b7d967b318ba5e54d915",
                "032380ace739422ae023a7259ae759529abbeafffb6cb557fa60f6679625f8f319",
                "0319223fc1af39cb95fded1f9804aa31ee3e3c2264388821e0bfa75a990775a0b5",
                "039c736367f6a2a3df7440bc7ba5d9d8bfbbe0e70afb09135619c070f1e7e0984b",
                "0202a5805c4e1f96ff33ee7af9d154a021865dd57103d8e635e29b51092ed7a99d",
                "039f6f65ea54eac792312ed32c52e8022f9efe509851ea7ebf13dd928ac047aa7c",
                "030eac5834b84d7604eadd1c6f1a566c6bb5f4181a727261e423c6f366208b39e9"
                };
            Dictionary<TransactionType, Fixed8> sys_fee = new Dictionary<TransactionType, Fixed8>
            {
                [TransactionType.BillTransaction] = Fixed8.One,
                [TransactionType.ReplyTransaction] = Fixed8.One,
                [TransactionType.AskTransaction] = Fixed8.One,
                [TransactionType.NFTDonateTransaction] = Fixed8.One*10,
                [TransactionType.BookTransferTransaction] = Fixed8.One*10,
                [TransactionType.BookSectionTransaction] = Fixed8.One * 100,
                [TransactionType.BookTransaction] = Fixed8.One * 1000,
                [TransactionType.TreatyTransaction] = Fixed8.One * 100,
                [TransactionType.NFTCoinTransaction] = Fixed8.One * 100,
                [TransactionType.SecretLetterTransaction] = Fixed8.One * 100,
                //[TransactionType.EnrollmentTransaction] = Fixed8.One * 1000,
                [TransactionType.IssueTransaction] = Fixed8.One * 5000,
                //[TransactionType.PublishTransaction] = Fixed8.One * 500,
                [TransactionType.RegisterTransaction] = Fixed8.One * 100000
            };

            this.SystemFee = sys_fee;
            this.SecondsPerBlock = 15;
            this.BappDetainOXS = Fixed8.One * 100_000;
            this.LowPriorityThreshold = Fixed8.Satoshi * 100_000;
        }
        public IEnumerable<IPEndPoint> GetSeedIPs()
        {
            foreach (string hostAndPort in this.SeedList)
            {
                string[] p = hostAndPort.Split(':');
                yield return LocalNode.GetIPEndpointFromHostPort(p[0], int.Parse(p[1]));
            }
        }
        public static void InitSeed(string[] seeds, bool onlySeed = false)
        {
            _default.OnlySeed = onlySeed;
            _default.SeedList = seeds;
        }
    }
}
