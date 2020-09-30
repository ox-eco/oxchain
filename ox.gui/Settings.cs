using Microsoft.Extensions.Configuration;
using OX.Network.P2P;
using System.Linq;
using System.Net;

namespace OX
{


    internal partial class Settings
    {

        public PathsSettings Paths { get; }
        public P2PSettings P2P { get; }
        public RPCSettings RPC { get; }
        public string PluginURL { get; }
        public SeedSettings SeedNode { get; }
        //public static Settings Default { get; }
        //static Settings()
        //{
        //    Default = new Settings();
        //}

        public Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            this.Paths = new PathsSettings(section.GetSection("Paths"));
            this.P2P = new P2PSettings(section.GetSection("P2P"));
            this.RPC = new RPCSettings(section.GetSection("RPC"));
            this.SeedNode = new SeedSettings(section.GetSection("Seeds"));
        }
    }

    internal class PathsSettings
    {
        public string Chain { get; }
        public string BizChain { get; }
        public string Index { get; }
        public string CertCache { get; }

        public PathsSettings(IConfigurationSection section)
        {
            this.Chain = string.Format(section.GetSection("Chain").Value, Message.Magic.ToString("X8"));
            this.BizChain = string.Format(section.GetSection("BizChain").Value, Message.Magic.ToString("X8"));
            this.Index = string.Format(section.GetSection("Index").Value, Message.Magic.ToString("X8"));
            this.CertCache = section.GetSection("CertCache").Value;
        }
    }

    internal class P2PSettings
    {
        public ushort Port { get; }
        public ushort WsPort { get; }
        public bool OnlySeed { get; }
        public int MinDesiredConnections { get; }
        public int MaxConnections { get; }
        public int MaxConnectionsPerAddress { get; }

        public P2PSettings(IConfigurationSection section)
        {
            this.Port = ushort.Parse(section.GetSection("Port").Value);
            this.WsPort = ushort.Parse(section.GetSection("WsPort").Value);
            string onlyconnectseed = section.GetValue("OnlySeed", "false");
            this.OnlySeed = onlyconnectseed.ToLower() == "true";
            this.MinDesiredConnections = section.GetValue("MinDesiredConnections", Peer.DefaultMinDesiredConnections);
            this.MaxConnections = section.GetValue("MaxConnections", Peer.DefaultMaxConnections);
            this.MaxConnectionsPerAddress = section.GetValue("MaxConnectionsPerAddress", 3);
        }
    }
    internal class SeedSettings
    {
        public string[] Seeds { get; }

        public SeedSettings(IConfigurationSection section)
        {
            Seeds = section.GetChildren().Select(p => p.Get<string>()).ToArray();
        }
    }
    internal class RPCSettings
    {
        public IPAddress BindAddress { get; }
        public ushort Port { get; }
        public string SslCert { get; }
        public string SslCertPassword { get; }
        public Fixed8 MaxGasInvoke { get; }

        public RPCSettings(IConfigurationSection section)
        {
            this.BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            this.Port = ushort.Parse(section.GetSection("Port").Value);
            this.SslCert = section.GetSection("SslCert").Value;
            this.SslCertPassword = section.GetSection("SslCertPassword").Value;
            this.MaxGasInvoke = Fixed8.Parse(section.GetValue("MaxGasInvoke", "0"));
        }
    }


}
