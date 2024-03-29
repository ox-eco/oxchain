﻿using OX.Cryptography;
//using OX.Properties;
using OX.Wallets;
using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OX.UI
{
    internal partial class SigningDialog : Form
    {
        class WalletEntry
        {
            public WalletAccount Account;

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Account.Label))
                {
                    return $"[{Account.Label}] " + Account.Address;
                }

                return Account.Address;
            }
        }


        public SigningDialog()
        {
            InitializeComponent();

            cmbFormat.SelectedIndex = 0;
            cmbAddress.Items.AddRange
                (
                Program.CurrentWallet.GetAccounts()
                .Where(u => u.HasKey)
                .Select(u => new WalletEntry() { Account = u })
                .ToArray()
                );

            if (cmbAddress.Items.Count > 0)
            {
                cmbAddress.SelectedIndex = 0;
            }
            else
            {
                textBox2.Enabled = false;
                button1.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show(LanHelper.LocalLanguage("You must input JSON object pending signature data."));
                return;
            }

            byte[] raw, signedData = null;
            try
            {
                switch (cmbFormat.SelectedIndex)
                {
                    case 0: raw = Encoding.UTF8.GetBytes(textBox1.Text); break;
                    case 1: raw = textBox1.Text.HexToBytes(); break;
                    default: return;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var account = (WalletEntry)cmbAddress.SelectedItem;
            var keys = account.Account.GetKey();

            try
            {
                signedData = Crypto.Default.Sign(raw, keys.PrivateKey, keys.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            textBox2.Text = signedData?.ToHexString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }
    }
}
