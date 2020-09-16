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

namespace OX.Bapps
{
    public abstract class Bapp
    {
        public static readonly List<Bapp> bapps = new List<Bapp>();

        private static readonly string BappsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "bapps");
        protected static OXSystem System { get; private set; }
        public static string KernelVersion => System.GetType().Assembly.GetVersion();
        public virtual string Name => GetType().Name;
        public abstract string[] BizAddresses { get; }
        public abstract string MatchKernelVersion { get; }
        public bool IsMatchKernel => KernelVersion == MatchKernelVersion;
        UInt160[] _bizScriptHashs;
        public UInt160[] BizScriptHashs
        {
            get
            {
                if (_bizScriptHashs.IsNullOrEmpty())
                {
                    _bizScriptHashs = BizAddresses.Select(m => m.ToScriptHash()).ToArray();
                }
                return _bizScriptHashs;
            }
        }
        static Bapp()
        {
            if (Directory.Exists(BappsRootPath))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        protected Bapp()
        {
            if (IsMatchKernel)
            {
                bapps.Add(this);
            }
        }
        public static T GetBapp<T>() where T : Bapp
        {
            var instance = bapps.Where(m => m is T)?.FirstOrDefault();
            if (instance.IsNotNull())
                return instance as T;
            return default;
        }
        public static bool ContainBizScriptHash<T>(UInt160 scriptHash) where T : Bapp
        {
            var sys = Bapp.GetBapp<T>();
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

        internal static void LoadBapps(OXSystem system)
        {
            System = system;
            if (!Directory.Exists(BappsRootPath)) return;
            foreach (var pathName in Directory.EnumerateDirectories(BappsRootPath))
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
                        if (!type.IsSubclassOf(typeof(Bapp))) continue;
                        if (type.IsAbstract) continue;

                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        try
                        {
                            constructor?.Invoke(null);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log(nameof(Bapp), LogLevel.Error, $"Failed to initialize bapp: {ex.Message}");
                        }
                    }
                    //}
                }
            }

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
                Plugin.Log(nameof(Bapp), LogLevel.Error, $"Failed to resolve assembly or its dependency: {ex.Message}");
                return null;
            }
        }
    }
}
