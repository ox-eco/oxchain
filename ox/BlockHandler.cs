using Akka.Actor;
using OX.Bapps;
using OX.Cryptography.ECC;
using OX.Ledger;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.SmartContract;
using OX.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OX
{

    public abstract class BlockHandler : UntypedActor
    {
        protected static Props _Instance = default;
        public class WalletCommand { public Wallet Wallet; }
        public OXSystem oxsystem { get; protected set; }
        public Wallet wallet { get; protected set; }
        public WalletAccount Account { get; protected set; }
        public KeyPair KeyPair { get; protected set; }
        public ECPoint PublicKey { get; protected set; }

        public List<UInt160> Permits = new List<UInt160>();
        public abstract string[] BizAddresses { get; }
        public abstract void OnStart();
        public abstract void OnStop();
        protected abstract void OnReceived(object message);
        protected abstract void OnBlockPersistCompleted(Block block);
        protected abstract void OnFlashStateCaptured(FlashState flashState);

        public BlockHandler(OXSystem system) : this(system, null)
        {
        }
        public BlockHandler(OXSystem system, Wallet wallet)
        {
            this.oxsystem = system;
            if (wallet != default)
            {
                this.wallet = wallet;
                WalletAccount walletAccount = wallet.GetHeldAccounts().FirstOrDefault();
                this.Account = walletAccount;
                this.KeyPair = walletAccount.GetKey();
                this.PublicKey = this.KeyPair.PublicKey;
            }
            LoadPermits();
        }

        void LoadPermits()
        {
            this.Permits.Clear();
            foreach (var ad in BizAddresses)
            {
                var sh = ad.ToScriptHash();
                if (Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance, out Fixed8 askFee))
                {
                    this.Permits.Add(sh);
                }
            }
        }

        protected override void OnReceive(object message)
        {
            if (message is Blockchain.PersistCompleted completed)
            {
                OnBlockPersistCompleted(completed.Block);
            }
            else if (message is Blockchain.FlashStateCaptured flashStateCaptured)
            {
                Bapp.OnFlashStateCaptured(flashStateCaptured.FlashState);
                OnFlashStateCaptured(flashStateCaptured.FlashState);
            }
            else if (message is WalletCommand walletCommand)
            {
                this.wallet = walletCommand.Wallet;
                WalletAccount walletAccount = walletCommand.Wallet.GetHeldAccounts().FirstOrDefault();
                this.KeyPair = walletAccount.GetKey();
                this.PublicKey = this.KeyPair.PublicKey;
            }
            else if (message is IInventory inventory)
            {
                this.Relay(inventory);
            }
            else
            {
                OnReceived(message);
            }
        }
        public virtual void Start()
        {
            this.OnStart();
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.FlashStateCaptured));
        }
        public virtual void Stop()
        {
            Context.System.EventStream.Unsubscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Unsubscribe(Self, typeof(Blockchain.FlashStateCaptured));
            this.OnStop();
        }
        public void Relay(IInventory inventory)
        {
            this.oxsystem.LocalNode.Tell(new LocalNode.Relay { Inventory = inventory });
        }
        public void SendDirectly(IInventory inventory)
        {
            this.oxsystem.LocalNode.Tell(new LocalNode.SendDirectly { Inventory = inventory });
        }
        public void RelayFlash(FlashState flashState)
        {
            this.oxsystem.LocalNode.Tell(new LocalNode.RelayFlash { FlashState = flashState });
        }
        public bool SignAndRelay(Transaction tx)
        {
            ContractParametersContext context;
            try
            {
                context = new ContractParametersContext(tx);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error creating contract params: {ex}");
                throw;
            }
            this.wallet.Sign(context);
            string msg;
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                this.wallet.ApplyTransaction(tx);
                this.Relay(tx);
                msg = $"Signed and relayed transaction with hash={tx.Hash}";
                Console.WriteLine(msg);
                return true;
            }
            msg = $"Failed sending transaction with hash={tx.Hash}";
            Console.WriteLine(msg);
            return true;
        }
        public bool SignAndRelay(FlashState fs)
        {
            ContractParametersContext context;
            try
            {
                context = new ContractParametersContext(fs);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error creating contract params: {ex}");
                throw;
            }
            this.wallet.Sign(context);
            string msg;
            if (context.Completed)
            {
                fs.Witnesses = context.GetWitnesses();
                this.Relay(fs);
                msg = $"Signed and relayed flashstate with hash={fs.Hash}";
                Console.WriteLine(msg);
                return true;
            }
            msg = $"Failed sending flashstate with hash={fs.Hash}";
            Console.WriteLine(msg);
            return true;
        }
    }
}
