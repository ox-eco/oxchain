using OX.SmartContract;
using OX.VM;
using OX.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace OX.UI
{
    internal partial class CreateLockAccountDialog : Form
    {
        public CreateLockAccountDialog()
        {
            InitializeComponent();
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Where(p => !p.WatchOnly && p.Contract.Script.IsStandardContract()).Select(p => p.GetKey()).ToArray());
        }

        public Contract GetContract()
        {
            uint timestamp = dateTimePicker1.Value.ToTimestamp();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(GetKey().PublicKey);
                sb.EmitPush(timestamp);
                sb.EmitPush(true);
                var sh = UInt160.Parse("0x334b191cca29463a62ef69b790e015b2f7467383");
                sb.EmitAppCall(sh);
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public KeyPair GetKey()
        {
            return (KeyPair)comboBox1.SelectedItem;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0;
        }
    }
}
