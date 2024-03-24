using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.Plugins;
using OX.Wallets;
using OX.IO.Json;
using OX.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OX.SmartContract;
using OX.IO;
using static OX.Ledger.Blockchain;


namespace OX.Bapps
{
    public class SideScope : ISerializable
    {
        public UInt160 MasterAddress;
        public string Description;
        public virtual int Size => MasterAddress.Size + Description.GetVarSize();
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(MasterAddress);
            writer.WriteVarString(Description);
        }
        public void Deserialize(BinaryReader reader)
        {
            MasterAddress = reader.ReadSerializable<UInt160>();
            Description = reader.ReadVarString();
        }
    }
    public abstract class Bapp
    {
        public static event BappEventHandler<BappEvent> BappEvent;
        public static event BappEventHandler<CrossBappMessage> CrossBappMessage;
        public static event BappEventHandler<Block> BappBlockEvent;
        public static event BappEventHandler<FlashMessage> FlashStateEvent;
        public static event BappEventHandler BappRebuildIndex;
        static Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
        public static IEnumerable<Assembly> Assemblies { get { return assemblies.Values; } }
        static readonly List<Bapp> bapps = new List<Bapp>();
        public static IEnumerable<Bapp> AllBapps { get { return bapps.AsEnumerable(); } }

        static readonly string BappsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "bapps");
        public static string KernelVersion => System.GetType().Assembly.GetVersion();

        protected static OXSystem System { get; private set; }
        IFlashStateProvider _flashStateProvider;
        protected IFlashStateProvider FlashStateProvider
        {
            get
            {
                if (_flashStateProvider.IsNull())
                {
                    _flashStateProvider = BuildFlashStateProvider();
                    if (_flashStateProvider.IsNotNull()) _flashStateProvider.Bapp = this;
                }
                return _flashStateProvider;
            }
        }
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

        Dictionary<ECPoint, bool> _bizScriptHashState;
        public Dictionary<ECPoint, bool> BizScriptHashStates
        {
            get
            {
                if (_bizScriptHashState.IsNullOrEmpty())
                {
                    _bizScriptHashState = new Dictionary<ECPoint, bool>(); // BizAddresses.Select(m => m.ToScriptHash()).ToDictionary();
                    if (BizAddresses.IsNotNullAndEmpty())
                        foreach (var address in BizPublicKeys)
                        {
                            _bizScriptHashState[address] = false;
                        }
                }
                return _bizScriptHashState;
            }
        }
        public ECPoint[] ValidBizScriptHashs
        {
            get
            {
                var bs = BizScriptHashStates.Where(m => m.Value);
                if (bs.IsNullOrEmpty()) return default;
                return bs.Select(m => m.Key).ToArray();
            }
        }
        public abstract ECPoint[] BizPublicKeys { get; }
        public UInt160[] BizAddresses => BizPublicKeys.IsNullOrEmpty() ? default : BizPublicKeys.Select(m => Contract.CreateSignatureRedeemScript(m).ToScriptHash()).ToArray();
        public abstract string MatchKernelVersion { get; }
        public abstract IBappProvider BuildBappProvider();
        public abstract IFlashStateProvider BuildFlashStateProvider();
        public abstract IBappApi BuildBappApi();
        public abstract IBappUi BuildBappUi();
        public abstract SideScope[] GetSideScopes();

        static Bapp()
        {
            if (Directory.Exists(BappsRootPath))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        protected Bapp()
        {
            InitBapp();
            bapps.Add(this);
        }
        protected abstract void InitBapp();
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
        public static IEnumerable<IFlashStateProvider> AllFlashStateProviders()
        {
            foreach (var bapp in bapps)
            {
                if (bapp.FlashStateProvider.IsNotNull())
                {
                    yield return bapp.FlashStateProvider;
                }
            }
        }
        public static void OnFlashStateCaptured(FlashMessage flashState)
        {
            foreach (var bapp in bapps)
            {
                bapp.OnFlashState(flashState);
            }
            FlashStateEvent?.Invoke(flashState);
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
        public static void OnRebuildUI()
        {
            foreach (var bapp in bapps)
            {
                bapp.OnUiRebuild();
            }
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
            return sys.BizAddresses.Contains(scriptHash);
        }
        public bool IsBizTransaction(Transaction tx, out BizTransaction BT)
        {
            if (tx is BizTransaction bt)
            {
                if (this.BizScriptHashStates.IsNotNullAndEmpty() && this.BizScriptHashStates.Select(m => Contract.CreateSignatureRedeemScript(m.Key).ToScriptHash()).Contains(bt.BizScriptHash))
                {
                    BT = bt;
                    return true;
                }
            }
            BT = default;
            return false;
        }
        public bool ContainBizTransaction(Block block, out BizTransaction[] bts)
        {
            bts = default;
            bool find = false;
            List<BizTransaction> list = new List<BizTransaction>();
            foreach (var tx in block.Transactions)
            {
                if (tx is BizTransaction bt)
                {
                    if (this.BizScriptHashStates.IsNotNullAndEmpty() && this.BizScriptHashStates.Select(m => Contract.CreateSignatureRedeemScript(m.Key).ToScriptHash()).Contains(bt.BizScriptHash))
                    {
                        find = true;
                        list.Add(bt);
                    }
                }
            }
            if (find)
                bts = list.ToArray();
            return find;
        }
        internal static void ResetBappState()
        {
            foreach (var bapp in bapps)
            {
                bapp.setBappState();
            }
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
                    assemblies[assembly.FullName] = assembly;
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
        void setBappState()
        {
            foreach (var ad in BizScriptHashStates)
            {
                var pubkey = ad.Key;
                var sh = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
                BizScriptHashStates[pubkey] = Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance, out Fixed8 askFee);
            }
        }
        void OnFlashState(FlashMessage flashState)
        {
            if (this.FlashStateProvider.IsNotNull()) this.FlashStateProvider.OnFlashState(flashState);
            if (this.BappProvider.IsNotNull()) this.BappProvider.OnFlashState(flashState);
            if (this.BappApi.IsNotNull()) this.BappApi.OnFlashState(flashState);
            if (this.BappUi.IsNotNull()) this.BappUi.OnFlashState(flashState);
        }
        void OnBlock(Block block)
        {
            bool ok = false;
            foreach (var tx in block.Transactions)
            {
                bool ok2 = false;
                foreach (var reference in tx.References)
                {
                    if (this.BizScriptHashStates.IsNotNullAndEmpty() && this.BizScriptHashStates.Select(m => Contract.CreateSignatureRedeemScript(m.Key).ToScriptHash()).Contains(reference.Value.ScriptHash) && reference.Value.AssetId == Blockchain.OXS)
                    {
                        ok2 = true;
                        break;
                    }
                }
                foreach (var output in tx.Outputs)
                {
                    if (this.BizScriptHashStates.IsNotNullAndEmpty() && this.BizScriptHashStates.Select(m => Contract.CreateSignatureRedeemScript(m.Key).ToScriptHash()).Contains(output.ScriptHash) && output.AssetId == Blockchain.OXS)
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
                setBappState();
            }
            if (this.BappProvider.IsNotNull()) this.BappProvider.BeforeOnBlock(block);
            if (this.BappApi.IsNotNull()) this.BappApi.BeforeOnBlock(block);
            if (this.BappUi.IsNotNull()) this.BappUi.BeforeOnBlock(block);

            if (this.BappProvider.IsNotNull()) this.BappProvider.OnBlock(block);
            if (this.BappApi.IsNotNull()) this.BappApi.OnBlock(block);
            if (this.BappUi.IsNotNull()) this.BappUi.OnBlock(block);

            if (this.BappProvider.IsNotNull()) this.BappProvider.AfterOnBlock(block);
            if (this.BappApi.IsNotNull()) this.BappApi.AfterOnBlock(block);
            if (this.BappUi.IsNotNull()) this.BappUi.AfterOnBlock(block);

        }
        void OnRebuild()
        {
            this.BappProvider?.OnRebuild();
            this.BappUi?.OnRebuild();
        }
        void OnUiRebuild()
        {
            this.BappUi?.OnRebuild();
        }
        public virtual void PushEvent(BappEvent bappEvent)
        {
            this.BappProvider?.OnBappEvent(bappEvent);
            this.BappApi?.OnBappEvent(bappEvent);
            this.BappUi?.OnBappEvent(bappEvent);
            BappEvent?.Invoke(bappEvent);
        }
    }
}
