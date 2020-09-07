﻿using OX.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OX.UI
{
    public partial class TradeVerificationDialog : Form
    {
        public TradeVerificationDialog(IEnumerable<TransactionOutput> outputs)
        {
            InitializeComponent();
            txOutListBox1.SetItems(outputs);
        }
    }
}
