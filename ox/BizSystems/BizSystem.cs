using Microsoft.Extensions.Configuration;
using OX.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using OX.Plugins;
using OX.Wallets;
using OX.Ledger;

namespace OX.BizSystems
{
    public abstract class BizSystem
    {
        public static readonly List<BizSystem> BizSystems = new List<BizSystem>();
        public static readonly Dictionary<string, IBizParser> BizParsers = new Dictionary<string, IBizParser>();

        private static readonly string bizSystemRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "bizsystems");
        protected static OXSystem System { get; private set; }
        public static string KernelVersion => System.GetType().Assembly.GetVersion();
        public virtual string Name => GetType().Name;
        public Dictionary<string, IModule> Modules { get; private set; } = new Dictionary<string, IModule>();
        public List<UInt160> Permits = new List<UInt160>();
        public abstract string[] BizAddresses { get; }
        public abstract IBizParser BuildBizParse();
        public abstract void RegisterModules();
        public abstract string MatchKernelVersion { get; }
        public bool IsMatchKernel => KernelVersion == MatchKernelVersion;
        static BizSystem()
        {
            if (Directory.Exists(bizSystemRootPath))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        protected BizSystem()
        {
            if (IsMatchKernel)
            {
                BizSystems.Add(this);
            }
        }

        bool LoadPermits()
        {
            bool ok = false;
            this.Permits.Clear();
            if (this.BizAddresses.IsNullOrEmpty()) return true;
            foreach (var ad in BizAddresses)
            {
                var sh = ad.ToScriptHash();
                if (Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance))
                {
                    this.Permits.Add(sh);
                    ok = true;
                }
            }
            return ok;
        }


        public abstract void Configure();

        protected virtual void OnBizSystemsLoaded()
        {
        }
        public static T GetBizParser<T>() where T : class, IBizParser
        {
            var instance = BizParsers.Values.Where(m => m is T)?.FirstOrDefault();
            if (instance.IsNotNull())
                return instance as T;
            return default;
        }
        public static T GetBizSystem<T>() where T : BizSystem
        {
            var instance = BizSystems.Where(m => m is T)?.FirstOrDefault();
            if (instance.IsNotNull())
                return instance as T;
            return default;
        }
        public static bool ContainBizScriptHash<T>(UInt160 scriptHash) where T : BizSystem
        {
            var sys = BizSystem.GetBizSystem<T>();
            if (sys.IsNull()) return false;
            return sys.Permits.Contains(scriptHash);
        }
        public bool IsBizTransaction(Transaction tx, out BizTransaction BT)
        {
            if (tx is BizTransaction bt)
            {
                if (this.Permits.Contains(bt.BizScriptHash))
                {
                    BT = bt;
                    return true;
                }
            }
            BT = default;
            return false;
        }
      
        internal static void LoadBizSystems(OXSystem system)
        {
            System = system;
            if (!Directory.Exists(bizSystemRootPath)) return;
            foreach (var pathName in Directory.EnumerateDirectories(bizSystemRootPath))
            {
                var di = new DirectoryInfo(pathName);
                //if (uint.TryParse(di.Name, out uint magic))
                //{
                foreach (string filename in Directory.EnumerateFiles(pathName, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var file = File.ReadAllBytes(filename);
                    Assembly assembly = Assembly.Load(file);
                    foreach (Type type in assembly.ExportedTypes)
                    {
                        if (!type.IsSubclassOf(typeof(BizSystem))) continue;
                        if (type.IsAbstract) continue;

                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        try
                        {
                            constructor?.Invoke(null);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log(nameof(BizSystem), LogLevel.Error, $"Failed to initialize bizsystem: {ex.Message}");
                        }
                    }
                    //}
                }
            }

        }
        internal static void CheckPermit()
        {
            List<BizSystem> bizs = new List<BizSystem>();
            foreach (var sys in BizSystems)
            {
                if (sys.LoadPermits())
                {
                    var parser = sys.BuildBizParse();
                    if (parser.IsNotNull())
                        BizParsers[parser.GetType().FullName] = parser;
                    sys.Configure();
                    sys.RegisterModules();
                }
                else
                    bizs.Add(sys);
            }
            foreach (var b in bizs)
                BizSystems.Remove(b);
        }
        internal static void NotifyBizSystemsLoadedAfterSystemConstructed()
        {
            foreach (var plugin in BizSystems)
                plugin.OnBizSystemsLoaded();
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(".resources"))
                return null;

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            AssemblyName an = new AssemblyName(args.Name);
            string filename = an.Name + ".dll";

            try
            {
                return Assembly.LoadFrom(filename);
            }
            catch (Exception ex)
            {
                Plugin.Log(nameof(BizSystem), LogLevel.Error, $"Failed to resolve assembly or its dependency: {ex.Message}");
                return null;
            }
        }
    }
}
