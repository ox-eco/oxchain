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
using OX.BizSystems;

namespace OX.Bapps
{
    public abstract class Bapp
    {
        public static readonly List<Bapp> bapps = new List<Bapp>();

        private static readonly string BappsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "bapps");
        protected static OXSystem System { get; private set; }
        IBappProvider _bappProvider;
        protected IBappProvider BappProvider
        {
            get
            {
                if (_bappProvider.IsNull())
                {
                    _bappProvider = BuildBappProvider();
                    _bappProvider.Bapp = this;
                }
                return _bappProvider;
            }
        }
        public static string KernelVersion => System.GetType().Assembly.GetVersion();
        public virtual string Name => GetType().Name;

        public bool IsMatchKernel => KernelVersion == MatchKernelVersion;
        Dictionary<UInt160, bool> _bizScriptHashState;
        public Dictionary<UInt160, bool> BizScriptHashStates
        {
            get
            {
                if (_bizScriptHashState.IsNullOrEmpty())
                {
                    _bizScriptHashState = new Dictionary<UInt160, bool>(); // BizAddresses.Select(m => m.ToScriptHash()).ToDictionary();
                    foreach (var address in BizAddresses)
                    {
                        _bizScriptHashState[address.ToScriptHash()] = false;
                    }
                }
                return _bizScriptHashState;
            }
        }
        public abstract string[] BizAddresses { get; }
        public abstract string MatchKernelVersion { get; }
        public abstract IBappProvider BuildBappProvider();

        static Bapp()
        {
            if (Directory.Exists(BappsRootPath))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        protected Bapp()
        {
            bapps.Add(this);
            this.resetBappState();
        }
        #region static
        public static void OnBlockIndex(Block block)
        {
            foreach (var bapp in bapps)
            {
                bapp.OnBlock(block);
            }
        }
        public static void OnRebuildIndex()
        {
            foreach (var bapp in bapps)
            {
                bapp.OnRebuild();
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
            return false;//sys.Permits.Contains(scriptHash);
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

        #endregion
        void resetBappState()
        {
            foreach (var ad in BizScriptHashStates)
            {
                var sh = ad.Key;
                BizScriptHashStates[sh] = Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance);
            }
        }
        public void OnBlock(Block block)
        {
            bool ok = false;
            foreach (var tx in block.Transactions)
            {
                bool ok2 = false;
                foreach (var reference in tx.References)
                {
                    if (this.BizScriptHashStates.ContainsKey(reference.Value.ScriptHash) && reference.Value.AssetId == Blockchain.Singleton.OXS)
                    {
                        ok2 = true;
                        break;
                    }
                }
                if (ok2)
                {
                    ok = true;
                    break;
                }
            }
            if (ok)
            {
                resetBappState();
            }
            this.BappProvider?.OnBlock(block);
        }
        public void OnRebuild()
        {
            this.BappProvider?.OnRebuild();
        }
        public bool IsBizTransaction(Transaction tx, out BizTransaction BT)
        {
            //if (tx is BizTransaction bt)
            //{
            //    if (this.Permits.Contains(bt.BizScriptHash))
            //    {
            //        BT = bt;
            //        return true;
            //    }
            //}
            BT = default;
            return false;
        }
    }
}
