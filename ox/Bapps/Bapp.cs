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
        public static event BappEventHandler<BappEvent> BappEvent;
        public static event BappEventHandler<CrossBappMessage> CrossBappMessage;
        public static event BappEventHandler<Block> BappBlockEvent;
        public static event BappEventHandler BappRebuildIndex;
        static readonly List<Bapp> bapps = new List<Bapp>();

        static readonly string BappsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "bapps");
        public static string KernelVersion => System.GetType().Assembly.GetVersion();

        protected static OXSystem System { get; private set; }
        IBappProvider _bappProvider;
        protected IBappProvider BappProvider
        {
            get
            {
                if (_bappProvider.IsNull())
                {
                    _bappProvider = BuildBappProvider();
                    if (_bappProvider.IsNotNull()) _bappProvider.Bapp = this;
                }
                return _bappProvider;
            }
        }
        IBappApi _bappApi;
        protected IBappApi BappApi
        {
            get
            {
                if (_bappApi.IsNull())
                {
                    _bappApi = BuildBappApi();
                    if (_bappApi.IsNotNull()) _bappApi.Bapp = this;
                }
                return _bappApi;
            }
        }
        IBappUi _bappUi;
        protected IBappUi BappUi
        {
            get
            {
                if (_bappUi.IsNull())
                {
                    _bappUi = BuildBappUi();
                    if (_bappUi.IsNotNull()) _bappUi.Bapp = this;
                }
                return _bappUi;
            }
        }

        public virtual string Name => GetType().Name;

        public bool IsMatchKernel => KernelVersion == MatchKernelVersion;
        public bool IsActive => BizAddresses.IsNullOrEmpty() || BizScriptHashStates.ContainsValue(true);
        public bool IsValid => IsMatchKernel && IsActive;

        Dictionary<UInt160, bool> _bizScriptHashState;
        public Dictionary<UInt160, bool> BizScriptHashStates
        {
            get
            {
                if (_bizScriptHashState.IsNullOrEmpty())
                {
                    _bizScriptHashState = new Dictionary<UInt160, bool>(); // BizAddresses.Select(m => m.ToScriptHash()).ToDictionary();
                    if (BizAddresses.IsNotNullAndEmpty())
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
        public abstract IBappApi BuildBappApi();
        public abstract IBappUi BuildBappUi();
        public abstract void BeforeBlockPersistence(Block block);
        public abstract void AfterBlockPersistence(Block block);
        public abstract void BeforeBlockShow(Block block);
        public abstract void AfterBlockShow(Block block);
        public abstract void BeforeBlockApi(Block block);
        public abstract void AfterBlockApi(Block block);

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
        public static void PushCrossBappMessage(CrossBappMessage message)
        {
            foreach (var bapp in bapps)
            {
                bapp.BappProvider?.OnCrossBappMessage(message);
                bapp.BappApi?.OnCrossBappMessage(message);
                bapp.BappUi?.OnCrossBappMessage(message);
            }
            CrossBappMessage?.Invoke(message);
        }
        public static IEnumerable<KeyValuePair<string, IUIModule>> AllUIModules()
        {
            Dictionary<string, IUIModule> modules = new Dictionary<string, IUIModule>();
            foreach (var bapp in bapps)
            {
                if (bapp.BappUi.IsNotNull())
                {
                    foreach (var m in bapp.BappUi.Modules)
                        modules[m.ModuleName] = m;
                }
            }
            return modules;
        }
        public static IEnumerable<IBappUi> AllBappUis()
        {
            foreach (var bapp in bapps)
            {
                if (bapp.BappUi.IsNotNull())
                {
                    yield return bapp.BappUi;
                }
            }
        }
        public static IEnumerable<IBappApi> AllBappApis()
        {
            foreach (var bapp in bapps)
            {
                if (bapp.BappApi.IsNotNull())
                {
                    yield return bapp.BappApi;
                }
            }
        }
        public static IEnumerable<IBappProvider> AllBappProviders()
        {
            foreach (var bapp in bapps)
            {
                if (bapp.BappProvider.IsNotNull())
                {
                    yield return bapp.BappProvider;
                }
            }
        }
        public static void OnBlockIndex(Block block)
        {
            foreach (var bapp in bapps)
            {
                bapp.OnBlock(block);
            }
            BappBlockEvent?.Invoke(block);
        }
        public static void OnRebuildIndex()
        {
            foreach (var bapp in bapps)
            {
                bapp.OnRebuild();
            }
            BappRebuildIndex?.Invoke();
        }
        public static T GetBapp<T>() where T : Bapp
        {
            var instance = bapps.Where(m => m is T)?.FirstOrDefault();
            if (instance.IsNotNull())
                return instance as T;
            return default;
        }
        public static ProviderType GetBappProvider<BappType, ProviderType>() where BappType : Bapp where ProviderType : class, IBappProvider
        {
            var Bapp = GetBapp<BappType>();
            if (Bapp.IsNull()) return default;
            if (Bapp.BappProvider.IsNull()) return default;
            if (Bapp.BappProvider is ProviderType) return Bapp.BappProvider as ProviderType;
            return default;
        }
        public static ApiType GetBappApi<BappType, ApiType>() where BappType : Bapp where ApiType : class, IBappApi
        {
            var Bapp = GetBapp<BappType>();
            if (Bapp.IsNull()) return default;
            if (Bapp.BappApi.IsNull()) return default;
            if (Bapp.BappApi is ApiType) return Bapp.BappApi as ApiType;
            return default;
        }
        public static UiType GetBappUi<BappType, UiType>() where BappType : Bapp where UiType : class, IBappUi
        {
            var Bapp = GetBapp<BappType>();
            if (Bapp.IsNull()) return default;
            if (Bapp.BappUi.IsNull()) return default;
            if (Bapp.BappUi is UiType) return Bapp.BappUi as UiType;
            return default;
        }
        public static bool ContainBizScriptHash<T>(UInt160 scriptHash) where T : Bapp
        {
            var sys = Bapp.GetBapp<T>();
            if (sys.IsNull()) return false;
            return sys.BizAddresses.Contains(scriptHash.ToAddress());
        }
        public bool IsBizTransaction(Transaction tx, out BizTransaction BT)
        {
            if (tx is BizTransaction bt)
            {
                if (this.BizScriptHashStates.ContainsKey(bt.BizScriptHash))
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

        #endregion
        void resetBappState()
        {
            foreach (var ad in BizScriptHashStates)
            {
                var sh = ad.Key;
                BizScriptHashStates[sh] = Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance);
            }
        }
        void OnBlock(Block block)
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
                foreach (var output in tx.Outputs)
                {
                    if (this.BizScriptHashStates.ContainsKey(output.ScriptHash) && output.AssetId == Blockchain.Singleton.OXS)
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
            if (this.BappProvider.IsNotNull())
            {
                BeforeBlockPersistence(block);
                this.BappProvider.OnBlock(block);
                AfterBlockPersistence(block);
            }
            if (this.BappApi.IsNotNull())
            {
                BeforeBlockApi(block);
                this.BappApi.OnBlock(block);
                AfterBlockApi(block);
            }
            if (this.BappUi.IsNotNull())
            {
                BeforeBlockShow(block);
                this.BappUi.OnBlock(block);
                AfterBlockShow(block);
            }
        }
        void OnRebuild()
        {
            this.BappProvider?.OnRebuild();
        }
        public void PushEvent(BappEvent bappEvent)
        {
            this.BappProvider?.OnBappEvent(bappEvent);
            this.BappApi?.OnBappEvent(bappEvent);
            this.BappUi?.OnBappEvent(bappEvent);
            BappEvent?.Invoke(bappEvent);
        }
    }
}
