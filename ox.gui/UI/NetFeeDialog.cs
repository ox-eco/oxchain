﻿using System;
using System.Text;
using System.Windows.Forms;
//using OX.Properties;

namespace OX.UI
{
    public partial class NetFeeDialog : Form
    {
        Fixed8 SystemFee;
        Fixed8 NetFee;

        public NetFeeDialog(Fixed8 SystemFee, Fixed8 NetFee)
        {
            this.SystemFee = SystemFee;
            this.NetFee = NetFee;
            InitializeComponent();
            this.ControlBox = false;
            this.CenterToParent();
            ShowCost(SystemFee + NetFee);
        }

        private void ShowCost(Fixed8 fee)
        {
            StringBuilder sb = new StringBuilder(32);

            string content = sb.AppendFormat("{0} {1} {2}", fee.ToString(), "OXC", LanHelper.LocalLanguage("will be consumed, confirm?")).ToString();
            this.CostContext.Text = content;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
