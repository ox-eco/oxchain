using Akka.Actor;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
//using OX.Properties;
using OX.SmartContract;
using System;
using System.Windows.Forms;

namespace OX.UI
{
    internal partial class SigningTxDialog : Form
    {
        public SigningTxDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show(LanHelper.LocalLanguage("You must input JSON object pending signature data."));
                return;
            }
            ContractParametersContext context = ContractParametersContext.Parse(textBox1.Text);
            if (!Program.CurrentWallet.Sign(context))
            {
                MessageBox.Show(LanHelper.LocalLanguage("The private key that can sign the data is not found."));
                return;
            }
            textBox2.Text = context.ToString();
            if (context.Completed) button4.Visible = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ContractParametersContext context = ContractParametersContext.Parse(textBox2.Text);
            if (!(context.Verifiable is Transaction tx))
            {
                MessageBox.Show("Only support to broadcast transaction.");
                return;
            }
            tx.Witnesses = context.GetWitnesses();
            Program.OXSystem.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
            InformationBox.Show(tx.Hash.ToString(), LanHelper.LocalLanguage("Data broadcast success, the hash is shown as follows:"), LanHelper.LocalLanguage("Broadcast Success"));
            button4.Visible = false;
        }
    }
}
