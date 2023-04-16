using Akka.Actor;
using OX.Cryptography;
using OX.IO;
using OX.IO.Actors;
using OX.Ledger;
using OX.Network.P2P;
using OX.Network.P2P.Payloads;
using OX.Persistence;
//using OX.Properties;
using OX.SmartContract;
using OX.VM;
using OX.Wallets;
using OX.Wallets.NEP6;
using OX.Wallets.SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;
//using Settings = OX.Properties.Settings;
using VMArray = OX.VM.Types.Array;

namespace OX.UI
{
    internal partial class MainForm : Form
    {
        private static readonly UInt160 RecycleScriptHash = new[] { (byte)OpCode.PUSHT }.ToScriptHash();
        private bool balance_changed = false;
        private bool check_nep5_balance = false;
        private DateTime persistence_time = DateTime.MinValue;
        private IActorRef actor;
        private WalletIndexer indexer;

        public MainForm(XDocument xdoc = null)
        {
            InitializeComponent();

            toolStripProgressBar1.Maximum = (int)Blockchain.SecondsPerBlock;

            //if (xdoc != null)
            //{
            //    Version version = Assembly.GetExecutingAssembly().GetName().Version;
            //    Version latest = Version.Parse(xdoc.Element("update").Attribute("latest").Value);
            //    if (version < latest)
            //    {
            //        toolStripStatusLabel3.Tag = xdoc;
            //        toolStripStatusLabel3.Text += $": {latest}";
            //        toolStripStatusLabel3.Visible = true;
            //    }
            //}
        }

        private void AddAccount(WalletAccount account, bool selected = false)
        {
            ListViewItem item = listView1.Items[account.Address];
            if (item != null)
            {
                if (!account.WatchOnly && ((WalletAccount)item.Tag).WatchOnly)
                {
                    listView1.Items.Remove(item);
                    item = null;
                }
            }
            if (item == null)
            {
                string groupName = account.WatchOnly ? "watchOnlyGroup" : account.Contract.Script.IsSignatureContract() ? "standardContractGroup" : "nonstandardContractGroup";
                item = listView1.Items.Add(new ListViewItem(new[]
                {
                    new ListViewItem.ListViewSubItem
                    {
                        Name = "address",
                        Text = account.Address
                    },
                    new ListViewItem.ListViewSubItem
                    {
                        Name = "ans"
                    },
                    new ListViewItem.ListViewSubItem
                    {
                        Name = "anc"
                    }
                }, -1, listView1.Groups[groupName])
                {
                    Name = account.Address,
                    Tag = account
                });
            }
            item.Selected = selected;
        }

        private void AddTransaction(Transaction tx, uint? height, uint time)
        {
            int? confirmations = (int)Blockchain.Singleton.Height - (int?)height + 1;
            if (confirmations <= 0) confirmations = null;
            string confirmations_str = confirmations?.ToString() ?? LanHelper.LocalLanguage("unconfirmed");
            string txid = tx.Hash.ToString();
            if (listView3.Items.ContainsKey(txid))
            {
                listView3.Items[txid].Tag = height;
                listView3.Items[txid].SubItems["confirmations"].Text = confirmations_str;
            }
            else
            {
                listView3.Items.Insert(0, new ListViewItem(new[]
                {
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "time",
                                Text = time.ToDateTime().ToString()
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "hash",
                                Text = txid
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "confirmations",
                                Text = confirmations_str
                            },
                            //add transaction type to list by phinx
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "txtype",
                                Text = tx.Type.ToString()
                            }
                            //end

                        }, -1)
                {
                    Name = txid,
                    Tag = height
                });
            }
        }

        private void Blockchain_PersistCompleted(Blockchain.PersistCompleted e)
        {
            if (IsDisposed) return;

            persistence_time = DateTime.UtcNow;
            if (Program.CurrentWallet != null)
            {
                check_nep5_balance = true;
                if (Program.CurrentWallet.GetCoins().Any(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(Blockchain.OXS_Token.Hash)) == true)
                    balance_changed = true;
            }

            BeginInvoke(new Action(RefreshConfirmations));
        }

        private void ChangeWallet(Wallet wallet)
        {
            if (Program.CurrentWallet != null)
            {
                Program.CurrentWallet.WalletTransaction -= CurrentWallet_WalletTransaction;
                if (Program.CurrentWallet is IDisposable disposable)
                    disposable.Dispose();
            }
            Program.CurrentWallet = wallet;
            listView3.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                    foreach (var i in Program.CurrentWallet.GetTransactions().Select(p => snapshot.Transactions.TryGet(p)).Where(p => p.Transaction != null).Select(p => new
                    {
                        p.Transaction,
                        p.BlockIndex,
                        Time = snapshot.GetHeader(p.BlockIndex).Timestamp
                    }).OrderBy(p => p.Time))
                    {
                        AddTransaction(i.Transaction, i.BlockIndex, i.Time);
                    }
                Program.CurrentWallet.WalletTransaction += CurrentWallet_WalletTransaction;
            }
            修改密码CToolStripMenuItem.Enabled = Program.CurrentWallet is UserWallet;
            交易TToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            提取OXCCToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            signDataToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            requestCertificateToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            注册资产RToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            资产分发IToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            deployContractToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            invokeContractToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            选举EToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            DetainToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建新地址NToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            导入私钥IToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建智能合约SToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            listView1.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                foreach (WalletAccount account in Program.CurrentWallet.GetAccounts().ToArray())
                {
                    AddAccount(account);
                }
            }
            balance_changed = true;
            check_nep5_balance = true;
        }

        private void CurrentWallet_WalletTransaction(object sender, WalletTransactionEventArgs e)
        {
            balance_changed = true;
            BeginInvoke(new Action<Transaction, uint?, uint>(AddTransaction), e.Transaction, e.Height, e.Time);
        }

        private WalletIndexer GetIndexer()
        {
            if (indexer is null)
                indexer = new WalletIndexer(Settings.Default.Paths.Index);
            return indexer;
        }

        private void RefreshConfirmations()
        {
            foreach (ListViewItem item in listView3.Items)
            {
                uint? height = item.Tag as uint?;
                int? confirmations = (int)Blockchain.Singleton.Height - (int?)height + 1;
                if (confirmations <= 0) confirmations = null;
                item.SubItems["confirmations"].Text = confirmations?.ToString() ?? LanHelper.LocalLanguage("unconfirmed");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            actor = Program.OXSystem.ActorSystem.ActorOf(EventWrapper<Blockchain.PersistCompleted>.Props(Blockchain_PersistCompleted));
            Program.OXSystem.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (actor != null)
                Program.OXSystem.ActorSystem.Stop(actor);
            ChangeWallet(null);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            uint walletHeight = 0;

            if (Program.CurrentWallet != null)
            {
                walletHeight = (Program.CurrentWallet.WalletHeight > 0) ? Program.CurrentWallet.WalletHeight - 1 : 0;
            }

            lbl_height.Text = $"{walletHeight}/{Blockchain.Singleton.Height}/{Blockchain.Singleton.HeaderHeight}";

            lbl_count_node.Text = LocalNode.Singleton.ConnectedCount.ToString();
            TimeSpan persistence_span = DateTime.UtcNow - persistence_time;
            if (persistence_span < TimeSpan.Zero) persistence_span = TimeSpan.Zero;
            if (persistence_span > Blockchain.TimePerBlock)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                toolStripProgressBar1.Value = persistence_span.Seconds;
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            }
            if (Program.CurrentWallet != null)
            {
                if (Program.CurrentWallet.WalletHeight <= Blockchain.Singleton.Height + 1)
                {
                    if (balance_changed)
                        using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                        {
                            IEnumerable<Coin> coins = Program.CurrentWallet?.GetCoins().Where(p => !p.State.HasFlag(CoinState.Spent)) ?? Enumerable.Empty<Coin>();
                            Fixed8 bonus_available = snapshot.CalculateBonus(Program.CurrentWallet.GetUnclaimedCoins().Select(p => p.Reference));
                            Fixed8 bonus_unavailable = snapshot.CalculateBonus(coins.Where(p => p.State.HasFlag(CoinState.Confirmed) && p.Output.AssetId.Equals(Blockchain.OXS_Token.Hash)).Select(p => p.Reference), snapshot.Height + 1);
                            Fixed8 bonus = bonus_available + bonus_unavailable;
                            var assets = coins.GroupBy(p => p.Output.AssetId, (k, g) => new
                            {
                                Asset = snapshot.Assets.TryGet(k),
                                Value = g.Sum(p => p.Output.Value),
                                Claim = k.Equals(Blockchain.OXC_Token.Hash) ? bonus : Fixed8.Zero
                            }).ToDictionary(p => p.Asset.AssetId);
                            if (bonus != Fixed8.Zero && !assets.ContainsKey(Blockchain.OXC_Token.Hash))
                            {
                                assets[Blockchain.OXC_Token.Hash] = new
                                {
                                    Asset = snapshot.Assets.TryGet(Blockchain.OXC_Token.Hash),
                                    Value = Fixed8.Zero,
                                    Claim = bonus
                                };
                            }
                            var balance_ans = coins.Where(p => p.Output.AssetId.Equals(Blockchain.OXS_Token.Hash)).GroupBy(p => p.Output.ScriptHash).ToDictionary(p => p.Key, p => p.Sum(i => i.Output.Value));
                            var balance_anc = coins.Where(p => p.Output.AssetId.Equals(Blockchain.OXC_Token.Hash)).GroupBy(p => p.Output.ScriptHash).ToDictionary(p => p.Key, p => p.Sum(i => i.Output.Value));
                            foreach (ListViewItem item in listView1.Items)
                            {
                                UInt160 script_hash = item.Name.ToScriptHash();
                                Fixed8 ans = balance_ans.ContainsKey(script_hash) ? balance_ans[script_hash] : Fixed8.Zero;
                                Fixed8 anc = balance_anc.ContainsKey(script_hash) ? balance_anc[script_hash] : Fixed8.Zero;
                                item.SubItems["ans"].Text = ans.ToString();
                                item.SubItems["anc"].Text = anc.ToString();
                            }
                            foreach (AssetState asset in listView2.Items.OfType<ListViewItem>().Select(p => p.Tag as AssetState).Where(p => p != null).ToArray())
                            {
                                if (!assets.ContainsKey(asset.AssetId))
                                {
                                    listView2.Items.RemoveByKey(asset.AssetId.ToString());
                                }
                            }
                            foreach (var asset in assets.Values)
                            {
                                string value_text = asset.Value.ToString() + (asset.Asset.AssetId.Equals(Blockchain.OXC_Token.Hash) ? $"+({asset.Claim})" : "");
                                if (listView2.Items.ContainsKey(asset.Asset.AssetId.ToString()))
                                {
                                    listView2.Items[asset.Asset.AssetId.ToString()].SubItems["value"].Text = value_text;
                                }
                                else
                                {
                                    string asset_name = asset.Asset.AssetType == AssetType.GoverningToken ? "OXS" :
                                                        asset.Asset.AssetType == AssetType.UtilityToken ? "OXC" :
                                                        asset.Asset.GetName();
                                    listView2.Items.Add(new ListViewItem(new[]
                                    {
                                        new ListViewItem.ListViewSubItem
                                        {
                                            Name = "name",
                                            Text = asset_name
                                        },
                                        new ListViewItem.ListViewSubItem
                                        {
                                            Name = "type",
                                            Text = asset.Asset.AssetType.ToString()
                                        },
                                        new ListViewItem.ListViewSubItem
                                        {
                                            Name = "value",
                                            Text = value_text
                                        },
                                        new ListViewItem.ListViewSubItem
                                        {
                                            ForeColor = Color.Gray,
                                            Name = "issuer",
                                            Text = $"{LanHelper.LocalLanguage("unknown issuer")}[{asset.Asset.Owner}]"
                                        }
                                    }, -1, listView2.Groups["unchecked"])
                                    {
                                        Name = asset.Asset.AssetId.ToString(),
                                        Tag = asset.Asset,
                                        UseItemStyleForSubItems = false
                                    });
                                }
                            }
                            balance_changed = false;
                        }
                    foreach (ListViewItem item in listView2.Groups["unchecked"].Items.OfType<ListViewItem>().ToArray())
                    {
                        ListViewItem.ListViewSubItem subitem = item.SubItems["issuer"];
                        AssetState asset = (AssetState)item.Tag;
                        CertificateQueryResult result;
                        if (asset.AssetType == AssetType.GoverningToken || asset.AssetType == AssetType.UtilityToken)
                        {
                            result = new CertificateQueryResult { Type = CertificateQueryResultType.System };
                        }
                        else
                        {
                            result = CertificateQueryService.Query(asset.Owner);
                        }
                        using (result)
                        {
                            subitem.Tag = result.Type;
                            switch (result.Type)
                            {
                                case CertificateQueryResultType.Querying:
                                case CertificateQueryResultType.QueryFailed:
                                    break;
                                case CertificateQueryResultType.System:
                                    subitem.ForeColor = Color.Green;
                                    subitem.Text = LanHelper.LocalLanguage("OX system");
                                    break;
                                case CertificateQueryResultType.Invalid:
                                    subitem.ForeColor = Color.Red;
                                    subitem.Text = $"[{ LanHelper.LocalLanguage("Invalid")}][{asset.Owner}]";
                                    break;
                                case CertificateQueryResultType.Expired:
                                    subitem.ForeColor = Color.Yellow;
                                    subitem.Text = $"[{LanHelper.LocalLanguage("Expired")}]{result.Certificate.Subject}[{asset.Owner}]";
                                    break;
                                case CertificateQueryResultType.Good:
                                    subitem.ForeColor = Color.Black;
                                    subitem.Text = $"{result.Certificate.Subject}[{asset.Owner}]";
                                    break;
                            }
                            switch (result.Type)
                            {
                                case CertificateQueryResultType.System:
                                case CertificateQueryResultType.Missing:
                                case CertificateQueryResultType.Invalid:
                                case CertificateQueryResultType.Expired:
                                case CertificateQueryResultType.Good:
                                    item.Group = listView2.Groups["checked"];
                                    break;
                            }
                        }
                    }
                }
                if (check_nep5_balance && persistence_span > TimeSpan.FromSeconds(2))
                {
                    UInt160[] addresses = Program.CurrentWallet.GetAccounts().Select(p => p.ScriptHash).ToArray();
                    foreach (string s in Settings.Default.NEP5Watched)
                    {
                        UInt160 script_hash = UInt160.Parse(s);
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            foreach (UInt160 address in addresses)
                                sb.EmitAppCall(script_hash, "balanceOf", address);
                            sb.Emit(OpCode.DEPTH, OpCode.PACK);
                            sb.EmitAppCall(script_hash, "decimals");
                            sb.EmitAppCall(script_hash, "name");
                            script = sb.ToArray();
                        }
                        ApplicationEngine engine = ApplicationEngine.Run(script);
                        if (engine.State.HasFlag(VMState.FAULT)) continue;
                        string name = engine.ResultStack.Pop().GetString();
                        byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
                        BigInteger amount = ((VMArray)engine.ResultStack.Pop()).Aggregate(BigInteger.Zero, (x, y) => x + y.GetBigInteger());
                        if (amount == 0)
                        {
                            listView2.Items.RemoveByKey(script_hash.ToString());
                            continue;
                        }
                        BigDecimal balance = new BigDecimal(amount, decimals);
                        string value_text = balance.ToString();
                        if (listView2.Items.ContainsKey(script_hash.ToString()))
                        {
                            listView2.Items[script_hash.ToString()].SubItems["value"].Text = value_text;
                        }
                        else
                        {
                            listView2.Items.Add(new ListViewItem(new[]
                            {
                                new ListViewItem.ListViewSubItem
                                {
                                    Name = "name",
                                    Text = name
                                },
                                new ListViewItem.ListViewSubItem
                                {
                                    Name = "type",
                                    Text = "NEP-5"
                                },
                                new ListViewItem.ListViewSubItem
                                {
                                    Name = "value",
                                    Text = value_text
                                },
                                new ListViewItem.ListViewSubItem
                                {
                                    ForeColor = Color.Gray,
                                    Name = "issuer",
                                    Text = $"ScriptHash:{script_hash}"
                                }
                            }, -1, listView2.Groups["checked"])
                            {
                                Name = script_hash.ToString(),
                                UseItemStyleForSubItems = false
                            });
                        }
                    }
                    check_nep5_balance = false;
                }
            }
        }

        private void 创建钱包数据库NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateWalletDialog dialog = new CreateWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                NEP6Wallet wallet = new NEP6Wallet(GetIndexer(), dialog.WalletPath);
                wallet.Unlock(dialog.Password);
                wallet.CreateAccount();
                wallet.Save();
                ChangeWallet(wallet);
                Settings.Default.LastWalletPath = dialog.WalletPath;
                Settings.Default.Save();
            }
        }

        private void 打开钱包数据库OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenWalletDialog dialog = new OpenWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                string path = dialog.WalletPath;
                Wallet wallet;
                if (Path.GetExtension(path) == ".db3")
                {
                    if (MessageBox.Show(LanHelper.LocalLanguage("Opening wallet files in older versions, update to newest format?Note: updated files cannot be openned by clients in older versions!"), LanHelper.LocalLanguage("Migrate Wallet"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                    {
                        string path_old = path;
                        path = Path.ChangeExtension(path_old, ".json");
                        NEP6Wallet nep6wallet;
                        try
                        {
                            nep6wallet = NEP6Wallet.Migrate(GetIndexer(), path, path_old, dialog.Password);
                        }
                        catch (CryptographicException)
                        {
                            MessageBox.Show(LanHelper.LocalLanguage("Password Incorrect"));
                            return;
                        }
                        nep6wallet.Save();
                        nep6wallet.Unlock(dialog.Password);
                        wallet = nep6wallet;
                        MessageBox.Show($"{LanHelper.LocalLanguage("Wallet file relocated. New wallet file has been saved at: ")}\n{path}");
                    }
                    else
                    {
                        try
                        {
                            wallet = UserWallet.Open(GetIndexer(), path, dialog.Password);
                        }
                        catch (CryptographicException)
                        {
                            MessageBox.Show(LanHelper.LocalLanguage("Password Incorrect"));
                            return;
                        }
                    }
                }
                else
                {
                    NEP6Wallet nep6wallet = new NEP6Wallet(GetIndexer(), path);
                    try
                    {
                        nep6wallet.Unlock(dialog.Password);
                    }
                    catch (CryptographicException)
                    {
                        MessageBox.Show(LanHelper.LocalLanguage("Password Incorrect"));
                        return;
                    }
                    wallet = nep6wallet;
                }
                ChangeWallet(wallet);
                Settings.Default.LastWalletPath = path;
                Settings.Default.Save();
            }
        }

        private void 修改密码CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ChangePasswordDialog dialog = new ChangePasswordDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                if (((UserWallet)Program.CurrentWallet).ChangePassword(dialog.OldPassword, dialog.NewPassword))
                    MessageBox.Show(LanHelper.LocalLanguage("Change password successful."));
                else
                    MessageBox.Show(LanHelper.LocalLanguage("Password Incorrect"));
            }
        }

        private void 重建钱包数据库RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            listView3.Items.Clear();
            GetIndexer().RebuildIndex();
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 转账TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Transaction tx;
            UInt160 change_address;
            Fixed8 fee;
            using (TransferDialog dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tx = dialog.GetTransaction();
                change_address = dialog.ChangeAddress;
                fee = dialog.Fee;
            }
            if (tx is InvocationTransaction itx)
            {
                using (InvokeContractDialog dialog = new InvokeContractDialog(itx))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    tx = dialog.GetTransaction(fee, change_address);
                }
            }
            Helper.SignAndShowInformation(tx);
        }

        private void 交易TToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (TradeForm form = new TradeForm())
            {
                form.ShowDialog();
            }
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningTxDialog dialog = new SigningTxDialog())
            {
                dialog.ShowDialog();
            }
        }
        private void DetainToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (Program.CurrentWallet.IsNull()) return;
            using (DetainDialog dialog = new DetainDialog())
            {
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                try
                {
                    var tx = dialog.GetTransaction();
                    if (tx.IsNotNull())
                        Helper.SignAndShowInformation(tx);
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }
        private void 提取OXCCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<ClaimForm>();
        }

        private void requestCertificateToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 注册资产RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvocationTransaction tx;
            using (AssetRegisterDialog dialog = new AssetRegisterDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tx = dialog.GetTransaction();
            }
            using (InvokeContractDialog dialog = new InvokeContractDialog(tx))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tx = dialog.GetTransaction(Fixed8.Zero);
            }

            try
            {
                Helper.SignAndShowInformation(tx);
            }
            catch
            {
                return;
            }

        }

        private void 资产分发IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (IssueDialog dialog = new IssueDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
                catch
                {
                    return;
                }
            }
        }

        private void deployContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvocationTransaction tx;
            try
            {
                using (DeployContractDialog dialog = new DeployContractDialog())
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    tx = dialog.GetTransaction();
                }
                using (InvokeContractDialog dialog = new InvokeContractDialog(tx))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    tx = dialog.GetTransaction(Fixed8.Zero);
                    Helper.SignAndShowInformation(tx);
                }
            }
            catch
            {
                return;
            }

        }

        private void invokeContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (InvokeContractDialog dialog = new InvokeContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    Helper.SignAndShowInformation(dialog.GetTransaction(Fixed8.Zero));
                }
                catch
                {
                    return;
                }
            }
        }

        private void 选举EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ElectionDialog dialog = new ElectionDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
                catch
                {
                    return;
                }
            }
        }

        private void signDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningDialog dialog = new SigningDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OptionsDialog dialog = new OptionsDialog())
            {
                dialog.ShowDialog();
            }
        }






        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            查看私钥VToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly &&
                ((WalletAccount)listView1.SelectedItems[0].Tag).Contract.Script.IsSignatureContract();
            viewContractToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly;
            voteToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly &&
                !string.IsNullOrEmpty(listView1.SelectedItems[0].SubItems["ans"].Text) &&
                decimal.Parse(listView1.SelectedItems[0].SubItems["ans"].Text) > 0;
            复制到剪贴板CToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            删除DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count > 0;
        }

        private void 创建新地址NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.SelectedIndices.Clear();
            WalletAccount account = Program.CurrentWallet.CreateAccount();
            AddAccount(account, true);
            if (Program.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
        }

        private void importWIFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportPrivateKeyDialog dialog = new ImportPrivateKeyDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listView1.SelectedIndices.Clear();
                foreach (string wif in dialog.WifStrings)
                {
                    WalletAccount account;
                    try
                    {
                        account = Program.CurrentWallet.Import(wif);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                    AddAccount(account, true);
                }
                if (Program.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
            }
        }

        private void importCertificateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SelectCertificateDialog dialog = new SelectCertificateDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listView1.SelectedIndices.Clear();
                WalletAccount account = Program.CurrentWallet.Import(dialog.SelectedCertificate);
                AddAccount(account, true);
                if (Program.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
            }
        }

        private void importWatchOnlyAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = InputBox.Show(LanHelper.LocalLanguage("Address"), LanHelper.LocalLanguage("Import Watch-Only Address"));
            if (string.IsNullOrEmpty(text)) return;
            using (StringReader reader = new StringReader(text))
            {
                while (true)
                {
                    string address = reader.ReadLine();
                    if (address == null) break;
                    address = address.Trim();
                    if (string.IsNullOrEmpty(address)) continue;
                    UInt160 scriptHash;
                    try
                    {
                        scriptHash = address.ToScriptHash();
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                    WalletAccount account = Program.CurrentWallet.CreateAccount(scriptHash);
                    AddAccount(account, true);
                }
            }
            if (Program.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
        }

        private void 多方签名MToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateMultiSigContractDialog dialog = new CreateMultiSigContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Contract contract = dialog.GetContract();
                if (contract == null)
                {
                    MessageBox.Show(LanHelper.LocalLanguage("Failed to add smart contract, corresponding private key missing in this wallet."));
                    return;
                }
                WalletAccount account = Program.CurrentWallet.CreateAccount(contract, dialog.GetKey());
                if (Program.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
                listView1.SelectedIndices.Clear();
                AddAccount(account, true);
            }
        }

        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateLockAccountDialog dialog = new CreateLockAccountDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Contract contract = dialog.GetContract();
                if (contract == null)
                {
                    MessageBox.Show(LanHelper.LocalLanguage("Failed to add smart contract, corresponding private key missing in this wallet."));
                    return;
                }
                WalletAccount account = Program.CurrentWallet.CreateAccount(contract, dialog.GetKey());
                if (Program.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
                listView1.SelectedIndices.Clear();
                AddAccount(account, true);
            }
        }

        private void 自定义CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportCustomContractDialog dialog = new ImportCustomContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Contract contract = dialog.GetContract();
                WalletAccount account = Program.CurrentWallet.CreateAccount(contract, dialog.GetKey());
                if (Program.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
                listView1.SelectedIndices.Clear();
                AddAccount(account, true);
            }
        }

        private void 查看私钥VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
            using (ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account))
            {
                dialog.ShowDialog();
            }
        }

        private void viewContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
            using (ViewContractDialog dialog = new ViewContractDialog(account.Contract))
            {
                dialog.ShowDialog();
            }
        }

        private void voteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
            using (VotingDialog dialog = new VotingDialog(account.ScriptHash))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
                catch
                {
                    return;
                }
            }
        }

        private void 复制到剪贴板CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(listView1.SelectedItems[0].Text);
            }
            catch (ExternalException) { }
        }

        private void 删除DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(LanHelper.LocalLanguage("Upon deletion, assets in these addresses will be permanently lost, are you sure to proceed?"), LanHelper.LocalLanguage("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            WalletAccount[] accounts = listView1.SelectedItems.OfType<ListViewItem>().Select(p => (WalletAccount)p.Tag).ToArray();
            foreach (WalletAccount account in accounts)
            {
                listView1.Items.RemoveByKey(account.Address);
                Program.CurrentWallet.DeleteAccount(account.ScriptHash);
            }
            if (Program.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
            balance_changed = true;
            check_nep5_balance = true;
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {

            删除DToolStripMenuItem1.Enabled = listView2.SelectedIndices.Count > 0;
            if (删除DToolStripMenuItem1.Enabled)
            {
                删除DToolStripMenuItem1.Enabled = listView2.SelectedItems.OfType<ListViewItem>().Select(p => p.Tag as AssetState).All(p => p == null || (p.AssetType != AssetType.GoverningToken && p.AssetType != AssetType.UtilityToken));
            }
        }



        private void 删除DToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0) return;
            var delete = listView2.SelectedItems.OfType<ListViewItem>().Select(p => p.Tag as AssetState).Where(p => p != null).Select(p => new
            {
                Asset = p,
                Value = Program.CurrentWallet.GetAvailable(p.AssetId)
            }).ToArray();
            if (delete.Length == 0) return;
            if (MessageBox.Show($"{LanHelper.LocalLanguage("Assets cannot be recovered once deleted, are you sure to delete the assets?")}\n"
                + string.Join("\n", delete.Select(p => $"{p.Asset.GetName()}:{p.Value}"))
                , LanHelper.LocalLanguage("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            ContractTransaction tx = Program.CurrentWallet.MakeTransaction(new ContractTransaction
            {
                Outputs = delete.Select(p => new TransactionOutput
                {
                    AssetId = p.Asset.AssetId,
                    Value = p.Value,
                    ScriptHash = RecycleScriptHash
                }).ToArray()
            }, fee: Fixed8.Zero);
            try
            {
                Helper.SignAndShowInformation(tx);
            }
            catch
            {
                return;
            }

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count == 0) return;
            Clipboard.SetDataObject(listView3.SelectedItems[0].SubItems[1].Text);
        }




        private void toolStripStatusLabel3_Click(object sender, EventArgs e)
        {

        }
    }
}
