using OX.IO;
using OX.Network.P2P.Payloads;
using OX.SmartContract;
using OX.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace OX.UI
{
    public partial class DetainDialog : Form
    {
        public DetainDialog()
        {
            InitializeComponent();
        }
        DetainTransaction transaction;
        public DetainTransaction GetTransaction()
        {
            DetainTransaction tx;
            BuildTransaction(out tx);
            return Program.CurrentWallet.MakeTransaction(tx);
        }

        private void ElectionDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Where(p => !p.WatchOnly && p.Contract.Script.IsStandardContract()).Select(p => p.Address).ToArray());
            comboBox2.Items.Add(DetainStatus.Freeze.ToString());
            comboBox2.Items.Add(DetainStatus.UnFreeze.ToString());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex >= 0)
            {
                BuildTransaction(out transaction);
            }
        }
        bool BuildTransaction(out DetainTransaction tx)
        {
            try
            {
                var d = uint.Parse(this.textBox1.Text);
                if (d < 100)
                {
                    tx = null;
                    this.button1.Enabled = false;
                    return false;
                }
                var f = uint.Parse(this.textBox2.Text);
                if (d > 1000)
                {
                    tx = null;
                    this.button1.Enabled = false;
                    return false;
                }
                var address = comboBox1.SelectedItem as string;
                var s = comboBox2.SelectedItem as string;
                var state = Enum.Parse<DetainStatus>(s);
                tx = new DetainTransaction(address.ToScriptHash())
                {
                    DetainDuration = d,
                    DetainState = state,
                    AskFee = Fixed8.OXU * f
                };
                label3.Text = $"{tx.SystemFee} OXC";
                this.button1.Enabled = true;
                return true;
            }
            catch (Exception e)
            {
                tx = null;
                this.button1.Enabled = false;
                return false;
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var s = textBox1.Text;
            if (!uint.TryParse(s, out uint v))
            {
                if (s.Length > 0)
                {
                    s = s.Substring(0, s.Length - 1);
                    this.textBox1.Clear();
                    this.textBox1.AppendText(s);
                }
            }
            else
            {
                if (!BuildTransaction(out transaction))
                {

                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            var s = textBox2.Text;
            if (!uint.TryParse(s, out uint v) && v > 1000)
            {
                if (s.Length > 0)
                {
                    s = s.Substring(0, s.Length - 1);
                    this.textBox2.Clear();
                    this.textBox2.AppendText(s);
                }
            }
            else
            {
                if (!BuildTransaction(out transaction))
                {

                }
            }
        }
    }
}
