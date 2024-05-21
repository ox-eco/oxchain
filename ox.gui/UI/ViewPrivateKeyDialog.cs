using OX.Ledger;
using OX.Wallets;
using System.Windows.Forms;

namespace OX.UI
{
    internal partial class ViewPrivateKeyDialog : Form
    {
        public ViewPrivateKeyDialog(WalletAccount account)
        {
            InitializeComponent();
            KeyPair key = account.GetKey();
            textBox3.Text = account.Address;
            textBox4.Text = key.PublicKey.EncodePoint(true).ToHexString();
            textBox1.Text = key.PrivateKey.ToHexString();
            textBox2.Text = key.Export();
            string msg = Blockchain.Singleton.IsFrozen(account.ScriptHash, out uint expireIndex) ? $"Frozen to {expireIndex}" : "Unfrozen";
            var ok = Blockchain.Singleton.VerifySlotValidator(account.Address.ToScriptHash(), out AccountState _, out Fixed8 _, out Fixed8 _);
            if (ok)
            {
                msg = $"  BziValidator Valid";
            }
            this.textBox5.Text = msg;
        }
    }
}
