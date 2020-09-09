using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.IO;
using Akka.Actor;
using OX.Ledger;
using OX.Network.P2P.Payloads;
using OX.IO;
using OX.IO.Data.LevelDB;
using OX.VM;
using OX.Cryptography;
using OX.IO.Caching;
using OX.Network;
using OX.Wallets;
using OX.Network.P2P;
using OX.SmartContract;
using System.Runtime.CompilerServices;
using OX.BizSystems;

namespace OX
{
    public abstract class BlockHandler : UntypedActor
    {
        public class WalletCommand { public Wallet Wallet; }
        public OXSystem oxsystem { get; protected set; }
        public Wallet wallet { get; protected set; }
        public WalletAccount Account { get; protected set; }
        public KeyPair KeyPair { get; protected set; }

        public List<UInt160> Permits = new List<UInt160>();
        public abstract string[] BizAddresses { get; }
        public abstract void OnStart();
        public abstract void OnStop();
        protected abstract void OnReceived(object message);
        protected abstract void OnBlockPersistCompleted(Block block);
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
            }
            LoadPermits();
        }
       
        void LoadPermits()
        {
            this.Permits.Clear();
            foreach (var ad in BizAddresses)
            {
                var sh = ad.ToScriptHash();
                if (Blockchain.Singleton.VerifyBizValidator(sh, out Fixed8 balance))
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
            else if (message is WalletCommand walletCommand)
            {
                this.wallet = walletCommand.Wallet;
                WalletAccount walletAccount = walletCommand.Wallet.GetHeldAccounts().FirstOrDefault();
                this.KeyPair = walletAccount.GetKey();
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
        }
        public virtual void Stop()
        {
            Context.System.EventStream.Unsubscribe(Self, typeof(Blockchain.PersistCompleted));
            this.OnStop();
        }
        public void Relay(IInventory inventory)
        {
            this.oxsystem.LocalNode.Tell(new LocalNode.Relay { Inventory = inventory });
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
    }
}
