using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Windows.Forms;
using PcPosSampleDll.Properties;
using System.Text;
using Sadad.PcPos.Core;
using System.Configuration;
using System.IO;
using Sadad.PcPos.Core.Common;
using System.Data.SQLite;

namespace PcPosSampleDll
{
    public partial class Main : Form
    {
        public PcPosBusiness PcPos;
        public PcPosDiscovery SearchPos = new PcPosDiscovery();
        public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        //private int[] retryTimeOut = new[] { 5000, 5000, 5000 };
        //private int[] responseTimeout = new[] { 20000, 5000, 5000 };

        private string dbName = "Transactions.db";
        private string DeviceInfoFlag = string.Empty;

        private string GetUnixTime()
        {
            return Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
        }

        private static bool CheckIpValid(string pIp)
        {
            IPAddress result;
            return
                !string.IsNullOrEmpty(pIp) &&
                IPAddress.TryParse(pIp, out result);
        }

        public Main()
        {
            InitializeComponent();

            CreatePcPosObject();

            Text += Resources.Version + AssemblyVersion;
            tabControl1.TabPages.Remove(tabPage6);
            cmbDeviceType.SelectedIndex = 0;

            SearchPos.OnSearchPcPos += test_OnSearchPcPos;
        }

        public sealed override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            GetSettings();
            cmbSearchType.SelectedIndex = 0;
            cmbxDivideType.SelectedIndex = 2;
            cmbConnectionType.SelectedIndex = 0;
            //var result = PcPos.GetSerialPort().ToArray();
            btnRefreshSerialPort_Click(this, e);

            cmbConnectionType.SelectedIndex = 0;
            radioButtonAsync.Checked = true;
            radioButtonSync.Checked = false;

            cmbLastTransactionType.SelectedIndex = 1;
            //btnSearchPos_Click(sender, e);

            dataGridViewMultiSaleId.Rows.Add(10);

            int counter = 1;
            foreach (DataGridViewRow row in dataGridViewMultiSaleId.Rows)
            {
                row.Cells[0].Value = counter.ToString();
                row.Cells[1].Value = "123456789012345678901234567890";
                row.Cells[2].Value = $"{counter++}00000";
            }

            CreateSqliteDb(dbName);
        }

        public List<DeviceElement> GetSettings()
        {
            var devices = new List<DeviceElement>();

            if (string.IsNullOrEmpty(Application.ExecutablePath)) return devices;
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            if (!fi.Exists)
            {
                MessageBox.Show("Config file doesn't exist");
                return devices;
            }

            try
            {
                Configuration config = null;
                if (fi.FullName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                }
                else
                {
                    ExeConfigurationFileMap map = new ExeConfigurationFileMap();
                    map.ExeConfigFilename = Application.ExecutablePath;
                    config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                }

                PcPosDevicesSection deviceSection = (PcPosDevicesSection)config.GetSection("pcPosDevices");
                foreach (DeviceElement dev in deviceSection.Devices)
                {
                    devices.Add(dev);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return devices;
        }

        public void SaveSettings(int baudRate, SadadStopBits stopBits)
        {
            FileInfo fi = new FileInfo(Application.ExecutablePath);
            if (!fi.Exists)
            {
                MessageBox.Show("File doesn't exist");
                return;
            }

            try
            {
                Configuration config = null;
                if (fi.FullName.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
                }
                else
                {
                    ExeConfigurationFileMap map = new ExeConfigurationFileMap();
                    map.ExeConfigFilename = Application.ExecutablePath;
                    config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                }

                var deviceSection = ((PcPosDevicesSection)config.GetSection("pcPosDevices"));
                List<DeviceElement> devices = new List<DeviceElement>();
                bool exist = false;
                foreach (DeviceElement item in deviceSection.Devices)
                {
                    devices.Add(item);
                    if (item.Name.Equals(cmbSerialPort.SelectedItem.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        exist = true;

                        item.Name = cmbSerialPort.SelectedItem.ToString();
                        item.DeviceType = ((int)Enum.Parse(typeof(DeviceType), cmbDeviceType.SelectedItem.ToString())).ToString();
                        item.SerialPort = cmbSerialPort.SelectedItem.ToString();
                        item.BaudRate = baudRate.ToString();
                        item.StopBits = ((int)stopBits).ToString();
                    }
                }

                if (!exist)
                {
                    deviceSection.Devices.Clear();
                    var element = new DeviceElement();

                    element.Name = cmbSerialPort.SelectedItem.ToString();
                    element.DeviceType = ((int)Enum.Parse(typeof(DeviceType), cmbDeviceType.SelectedItem.ToString())).ToString();
                    element.SerialPort = cmbSerialPort.SelectedItem.ToString();
                    element.BaudRate = baudRate.ToString();
                    element.StopBits = ((int)stopBits).ToString();
                    deviceSection.Devices.Add(element);

                    devices.ForEach(d =>
                    {
                        element = new DeviceElement();

                        element.Name = d.Name;
                        element.DeviceType = d.DeviceType;
                        element.SerialPort = d.SerialPort;
                        element.BaudRate = d.BaudRate;
                        element.StopBits = d.StopBits;
                        deviceSection.Devices.Add(element);
                    });
                }
                config.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void CleanResult()
        {
            PacketType.Text = "";
            ResponseCode.Text = "";
            Amount.Text = "";
            CardNo.Text = "";
            ProcessingCode.Text = "";
            TransactionNo.Text = "";
            TransactionTime.Text = "";
            transactionDate.Text = "";
            RRN.Text = "";
            ApprovalCode.Text = "";
            Terminal.Text = "";
            Merchant.Text = "";
            OptionalField.Text = "";
            PcPosStatus.Text = "";
            txtSaleOrderId.Text = "";


            BillPacketType.Text = "";
            BillProcessingCode.Text = "";
            BillAmountResponse.Text = "";
            BillCardNo.Text = "";
            BillResponseCode.Text = "";
            BillTransactionNo.Text = "";
            BillTransactionTime.Text = "";
            BilltransactionDate.Text = "";
            BillRRN.Text = "";
            BillApprovalCode.Text = "";
            BillTerminal.Text = "";
            BillMerchant.Text = "";
            BillOptionalField.Text = "";
            BillPcPosStatus.Text = "";


            txtPacketTypeInquiredSale.Text = "";
            txtInquiredSaleResponseCode.Text = "";
            txtInquiredSaleCardNo.Text = "";
            txtInquiredSaleProcessingCode.Text = "";
            txtInquiredSaleTransactionNo.Text = "";
            txtInquiredSaleTransactionTime.Text = "";
            txtInquiredSaleTransactionDate.Text = "";
            txtInquiredSaleSerialNo.Text = "";
            txtInquiredSaleAmountRes.Text = "";
            txtInquiredSaleRRN.Text = "";
            txtInquiredSaleApprovalCode.Text = "";
            txtInquiredSaleTerminal.Text = "";
            txtInquiredSaleMerchant.Text = "";
            txtInquiredSaleOptionalField.Text = "";
            txtInquiredSalePcPosStatus.Text = "";
        }

        private void CreatePcPosObject()
        {
            if (PcPos == null)
            {
                PcPos = new PcPosBusiness();
                PcPos.OnSaleResult += PcPosSaleResult;
                PcPos.OnBillPaymentResult += PcPosPayBillResult;
                PcPos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                PcPos.OnGovernmentInquiriedIdentifiedSaleResult += PcPos_OnOrganizationInquiriedIdentifiedSaleResult;
                PcPos.OnCardInfoResult += PcPos_OnCardInfoResult;
                PcPos.OnPosDeviceInfoResult += PcPos_OnPosDeviceInfoResult;
                PcPos.OnAccountInfoResult += PcPos_OnAccountInfoResult;
            }
        }

        // Return a list of the TreeNodes that are checked.
        private void FindCheckedDevices(List<PosDevice> checkedDevices, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                // Add this node.
                if (node.Checked)
                {
                    var device = node.Tag as PosDevice;
                    checkedDevices.Add(device);
                }

                // Check the node's descendants.
                FindCheckedDevices(checkedDevices, node.Nodes);
            }
        }

        // Return a list of the checked TreeView nodes.
        private List<PosDevice> CheckedDevices(TreeView trv)
        {
            List<PosDevice> checkedDevices = new List<PosDevice>();
            FindCheckedDevices(checkedDevices, trv.Nodes);
            return checkedDevices;
        }

        private void treeViewEx1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Checked)
                foreach (TreeNode node in treeViewEx1.Nodes)
                {
                    if (node != e.Node)
                        node.Checked = false;
                }
        }

        private void test_OnSearchPcPos(object sender, DiscoveryResult searchResult)
        {
            Invoke(new MethodInvoker(delegate
                {
                    foreach (PosDevice device in searchResult.Devices)
                    {
                        TreeNode mainNode = treeViewEx1.Nodes.Add(device.MerchantName);
                        mainNode.Tag = device;

                        TreeNode ipNode = mainNode.Nodes.Add(device.IpAddress);
                        TreeNode portNode = mainNode.Nodes.Add(device.Port);
                        TreeNode terminalNode = mainNode.Nodes.Add(device.TerminalId);
                        TreeNode merchantNode = mainNode.Nodes.Add(device.MerchantId);

                        treeViewEx1.HideCheckBox(ipNode);
                        treeViewEx1.HideCheckBox(portNode);
                        treeViewEx1.HideCheckBox(terminalNode);
                        treeViewEx1.HideCheckBox(merchantNode);

                        if (treeViewEx1.Nodes.Contains(mainNode))
                        {
                            continue;
                        }
                        treeViewEx1.Nodes.Add(mainNode);

                        //radTreeView1.Refresh();
                    }

                    if (treeViewEx1.Nodes.Count == 1)
                    {
                        treeViewEx1.Nodes[0].Checked = true;
                    }
                    btnSearchPos.Text = Resources.Search;
                }));
        }

        private void btnSearchPos_Click(object sender, EventArgs e)
        {
            if (btnSearchPos.Text.Equals(Resources.Search))
            {
                treeViewEx1.Nodes.Clear();
                switch (cmbSearchType.Text)
                {
                    case "همه":
                        SearchPos.SearchPcPos(100, 5000, PcPosOperationMode.Async);
                        break;

                    case "شماره ترمینال ":
                        SearchPos.SearchPcPosByTerminalId(100, 5000, txtSearchValue.Text, PcPosOperationMode.Async);
                        break;

                    case "شماره مشتری":
                        SearchPos.SearchPcPosByMerchantId(100, 5000, txtSearchValue.Text, PcPosOperationMode.Async);
                        break;

                    case "نام فروشگاه":
                        SearchPos.SearchPcPosByMerchantName(100, 5000, txtSearchValue.Text, PcPosOperationMode.Async);
                        break;

                    case "ادرس Ip":
                        SearchPos.SearchPcPosByIp(100, 5000, txtSearchValue.Text, PcPosOperationMode.Async);
                        break;
                    default:
                        SearchPos.SearchPcPos(100, 5000, PcPosOperationMode.Async);
                        break;
                }

                btnSearchPos.Text = Resources.Cancel_Search;
            }
            else
            {
                SearchPos.AborSearchOperation();
                btnSearchPos.Text = Resources.Search;
            }
        }

        private void btnSale_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnSale.Text == Resources.Buy)
                {
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = false;
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    #region Magic Transaction

                    if (cmbDeviceType.Text.Equals("Magic", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var mData = new MagicData();
                        mData.RequestId = txtOrderId.Text;
                        mData.Amount = txtPayAmount.Text;
                        mData.TerminalId = txtPayTerminalId.Text;
                        mData.MerchantId = txtPayMerchantId.Text;
                        mData.BillNo = txtPayId.Text;

                        PcPos.MagicData = mData;
                        PcPos.DeviceType = DeviceType.Magic;
                        PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnSaleResult += PcPosSaleResult;
                            PcPos.AsyncSaleTransaction();
                            btnSale.Text = Resources.Cancel_Buy;
                        }
                        else
                        {
                            btnSale.Text = Resources.Cancel_Buy;
                            PcPosSaleResult(sender, PcPos.SyncSaleTransaction());
                        }
                        return;
                    }

                    #endregion

                    //set transaction id
                    if (!string.IsNullOrEmpty(txtPayId.Text))
                        PcPos.SetSaleId(txtPayId.Text);
                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtOrderId.Text = GetUnixTime();
                    }
                    if (!string.IsNullOrEmpty(txtOrderId.Text))
                        PcPos.SetOrderId(txtOrderId.Text);
                    PcPos.SerialNo = txtPayPosSerialNo.Text;

                    #region Multi-Merchant Data

                    if (!string.IsNullOrEmpty(txtMultiAccount.Text))
                    {
                        var listTimeOutDic = new List<MultiAccountPosDivider>();
                        var listTimeOutDicEx = new List<MultiAccountPosDividerEx>();
                        foreach (var s in txtMultiAccount.Text.Split(','))
                        {
                            if (txtMultiAccount.Text.StartsWith("IR", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var mmpd = new MultiAccountPosDividerEx()
                                {
                                    Iban = s.Split(':')[0],
                                    Value = s.Split(':')[1]
                                };
                                listTimeOutDicEx.Add(mmpd);
                            }
                            else
                            {
                                var mmpd = new MultiAccountPosDivider()
                                {
                                    Index = int.Parse(s.Split(':')[0]),
                                    Value = s.Split(':')[1]
                                };
                                listTimeOutDic.Add(mmpd);
                            }
                        }

                        int multiAccountErrorCode = 0;
                        if (cmbxDivideType.SelectedItem.ToString().Contains("1")) // درصدی قدیم
                        {
                            if (txtMultiAccount.Text.StartsWith("IR", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDicEx, MultiAccountMode.PercentOld, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                            else
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentOld, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("2")) // مبلغی قدیم
                        {
                            if (txtMultiAccount.Text.StartsWith("IR", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDicEx, MultiAccountMode.AmountOld, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                            else
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountOld, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("3")) // درصدی جدید
                        {
                            if (txtMultiAccount.Text.StartsWith("IR", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDicEx, MultiAccountMode.PercentNew, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                            else
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentNew, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("4")) // مبلغی جدید
                        {
                            if (txtMultiAccount.Text.StartsWith("IR", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDicEx, MultiAccountMode.AmountNew, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                            else
                            {
                                if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountNew, out multiAccountErrorCode))
                                {
                                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                                    return;
                                }
                            }
                        }
                    }

                    #endregion

                    if (!string.IsNullOrEmpty(txtCardInfo.Text))
                    {
                        PcPos.SetCardInfo(txtCardInfo.Text);
                    }

                    PcPos.Amount = txtPayAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    //var ads = new List<KeyValuePair<string, string>>();
                    //ads.Add(new KeyValuePair<string, string>("نام بیمار2", "سجاد ابراهیمی اصل"));
                    //ads.Add(new KeyValuePair<string, string>("نام بیمار2", "محمود حسینی پور"));
                    //ads.Add(new KeyValuePair<string, string>("نام بیمار", "مجید عسگری نیاءءءءءءءء"));
                    //ads.Add(new KeyValuePair<string, string>("همراه بیمار", "علی ترووووووشه"));
                    //ads.Add(new KeyValuePair<string, string>("Text", "همراه بیمار تقاضا میشود هر چه سریعتر اقدام به تسویه حساب نماید در غیر اینصورت مسئولیتی در قبال موارد پیش آمده متوجه بیمارستان نمیباشد."));
                    PcPos.SetAdvertisement(txtAdvertisementData.Text);

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnSaleResult += PcPosSaleResult;
                            PcPos.AsyncSaleTransaction();
                            btnSale.Text = Resources.Cancel_Buy;
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnSale.Text = Resources.Cancel_Buy;
                            PcPosSaleResult(null, PcPos.SyncSaleTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnSaleResult += PcPosSaleResult;
                                PcPos.AsyncSaleTransaction();

                                btnSale.Text = Resources.Cancel_Buy;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnSale.Text = Resources.Cancel_Buy;
                                var res = PcPos.SyncSaleTransaction();
                                PcPosSaleResult(null, res);
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnSale.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                    btnSale.Text = Resources.Buy;
                }
            }
            catch (Exception ex)
            {
                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                btnSale.Text = Resources.Buy;
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPosSaleResult(object sender, PosResult pPosResult)
        {
            Action<PosResult> fillResult = delegate (PosResult e)
            {
                PacketType.Text = e.PacketType;
                ResponseCode.Text = e.ResponseCode;
                Amount.Text = e.Amount;
                CardNo.Text = e.CardNo;
                ProcessingCode.Text = e.ProcessingCode;
                TransactionNo.Text = e.TransactionNo;
                TransactionTime.Text = e.TransactionTime;
                transactionDate.Text = e.TransactionDate;
                RRN.Text = e.Rrn;
                ApprovalCode.Text = e.ApprovalCode;
                Terminal.Text = e.TerminalId;
                Merchant.Text = e.MerchantId;
                OptionalField.Text = e.OptionalField;
                PcPosStatus.Text = e.PcPosStatus;
                txtSaleOrderId.Text = e.OrderId;

                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                btnSale.Text = Resources.Buy;
                btnCommodityBasket.Text = Resources.Commodity_Basket;
                btnFoodSafety.Text = Resources.Food_Safety;

                btnGetCardInfo.Text = Resources.GetCardInfo;

                if (e.PcPosStatusCode != (int)FrameExchangeResponse.SimultaneousRequestError)
                {
                    //PcPos = null;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosResult);
                    SaveDb(pPosResult);
                }));
            }
            else
            {
                fillResult(pPosResult);
                SaveDb(pPosResult);
            }
        }

        private void btnBillPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnBillPayment.Text == Resources.Bill_Payment)
                {
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //set transaction id
                    PcPos.SetBillInfo(BillId.Text, BillPayId.Text);
                    PcPos.SerialNo = BillSerialNo.Text;

                    PcPos.Amount = BillAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnBillPaymentResult += PcPosPayBillResult;
                            btnBillPayment.Text = Resources.Cancel_Bill_Payment;
                            PcPos.AsyncBillPaymentTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnBillPayment.Text = Resources.Cancel_Bill_Payment;
                            PcPosPayBillResult(null, PcPos.SyncBillPaymentTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnBillPaymentResult += PcPosPayBillResult;
                                PcPos.AsyncBillPaymentTransaction();
                                btnBillPayment.Text = Resources.Cancel_Bill_Payment;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnBillPayment.Text = Resources.Cancel_Bill_Payment;
                                PcPosPayBillResult(null, PcPos.SyncBillPaymentTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                }
                else if (btnBillPayment.Text == Resources.Cancel_Bill_Payment)
                {
                    PcPos.AbortPcPosOperation();
                    btnBillPayment.Text = Resources.Bill_Payment;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPosPayBillResult(object sender, PosResult pPosResult)
        {
            Action<PosResult> fillResult = delegate (PosResult e)
            {
                BillPacketType.Text = e.PacketType;
                BillResponseCode.Text = e.ResponseCode;
                BillAmountResponse.Text = e.Amount;
                BillCardNo.Text = e.CardNo;
                BillProcessingCode.Text = e.ProcessingCode;
                BillTransactionNo.Text = e.TransactionNo;
                BillTransactionTime.Text = e.TransactionTime;
                BilltransactionDate.Text = e.TransactionDate;
                BillRRN.Text = e.Rrn;
                BillApprovalCode.Text = e.ApprovalCode;
                BillTerminal.Text = e.TerminalId;
                BillMerchant.Text = e.MerchantId;
                BillOptionalField.Text = e.OptionalField;
                BillPcPosStatus.Text = e.PcPosStatus;
                btnBillPayment.Text = Resources.Bill_Payment;

                if (e.PcPosStatusCode != (int)FrameExchangeResponse.SimultaneousRequestError)
                {
                    //PcPos = null;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosResult);
                    SaveDb(pPosResult);
                }));
            }
            else
            {
                fillResult(pPosResult);
                SaveDb(pPosResult);
            }
        }

        private void btnInquiredSalePayBill_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnInquiredSalePayBill.Text == Resources.Buy)
                {
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    if (string.IsNullOrEmpty(txtInquiredSaleAmount.Text))
                        PcPos.Amount = txtInquiredSaleAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    //set transaction id
                    PcPos.SetSaleId(txtInquiredSaleId.Text);
                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtInquiredOrderId.Text = GetUnixTime();
                    }
                    PcPos.SetOrderId(txtInquiredOrderId.Text);

                    PcPos.Amount = txtInquiredSaleAmount.Text;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                            btnInquiredSalePayBill.Text = Resources.Cancel_Buy;
                            PcPos.AsyncInquiriedIdentifiedTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnInquiredSalePayBill.Text = Resources.Cancel_Buy;
                            PcPos_OnInquiredSaleResult(null, PcPos.SyncInquiriedIdentifiedTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                                PcPos.AsyncInquiriedIdentifiedTransaction();
                                btnInquiredSalePayBill.Text = Resources.Cancel_Buy;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnInquiredSalePayBill.Text = Resources.Cancel_Buy;
                                PcPos_OnInquiredSaleResult(null, PcPos.SyncInquiriedIdentifiedTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnInquiredSalePayBill.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnInquiredSalePayBill.Text = Resources.Buy;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnFixedDuty_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnFixedDuty.Text == Resources.Buy)
                {
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //if (string.IsNullOrEmpty(txtInquiredSaleAmount.Text))
                    //    PcPos.Amount = txtInquiredSaleAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    //set fixed duty
                    PcPos.SetFixedDuty((int)numericProviderId.Value, (int)numericServiceCode.Value, (int)numericFixedDutyCount.Value);
                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtInquiredOrderId.Text = GetUnixTime();
                    }
                    PcPos.SetOrderId(txtInquiredOrderId.Text);

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                            btnFixedDuty.Text = Resources.Cancel_Buy;
                            PcPos.AsyncFixedDutyTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnFixedDuty.Text = Resources.Cancel_Buy;
                            PcPos_OnInquiredSaleResult(null, PcPos.SyncFixedDutyTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                                PcPos.AsyncFixedDutyTransaction();
                                btnFixedDuty.Text = Resources.Cancel_Buy;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnFixedDuty.Text = Resources.Cancel_Buy;
                                PcPos_OnInquiredSaleResult(null, PcPos.SyncFixedDutyTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnFixedDuty.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnFixedDuty.Text = Resources.FixDutyBuy;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPos_OnInquiredSaleResult(object sender, PosResult pPosResult)
        {
            Action<PosResult> fillResult = delegate (PosResult e)
            {
                txtPacketTypeInquiredSale.Text = e.PacketType;
                txtInquiredSaleResponseCode.Text = e.ResponseCode;
                txtInquiredSaleCardNo.Text = e.CardNo;
                txtInquiredSaleProcessingCode.Text = e.ProcessingCode;
                txtInquiredSaleTransactionNo.Text = e.TransactionNo;
                txtInquiredSaleTransactionTime.Text = e.TransactionTime;
                txtInquiredSaleTransactionDate.Text = e.TransactionDate;
                txtInquiredSaleAmountRes.Text = e.Amount;
                txtInquiredSaleRRN.Text = e.Rrn;
                txtInquiredSaleApprovalCode.Text = e.ApprovalCode;
                txtInquiredSaleTerminal.Text = e.TerminalId;
                txtInquiredSaleMerchant.Text = e.MerchantId;
                txtInquiredSaleOptionalField.Text = e.OptionalField;
                txtInquiredSalePcPosStatus.Text = e.PcPosStatus;
                txtInquiredOrderIdRes.Text = e.OrderId;
                btnInquiredSalePayBill.Text = Resources.Buy;
                btnFixedDuty.Text = Resources.FixDutyBuy;
                btnGetCardInfo.Text = Resources.GetCardInfo;

                if (e.PcPosStatusCode != (int)FrameExchangeResponse.SimultaneousRequestError)
                {
                    //PcPos = null;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosResult);
                    SaveDb(pPosResult);
                }));
            }
            else
            {
                fillResult(pPosResult);
                SaveDb(pPosResult);
            }
        }

        private void btnPayBillInquiredSaleOrganization_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnPayBillInquiredSaleOrganization.Text == Resources.Buy)
                {
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    if (radioOneGovSaleId.Checked)
                    {
                        if (!string.IsNullOrEmpty(txtSaleAmountInquiredSaleOrganization.Text))
                            PcPos.Amount = txtSaleAmountInquiredSaleOrganization.Text;
                        //set transaction id
                        PcPos.SetSaleId(txtSaleIdInquiredSaleOrganization.Text);
                    }
                    else
                    {
                        SaleIdProvider multiSaleId = new SaleIdProvider();
                        decimal sum = 0;
                        foreach (DataGridViewRow row in dataGridViewMultiSaleId.Rows)
                        {
                            try
                            {
                                var idx = 0;
                                var converted = int.TryParse(row.Cells[0].Value.ToString(), out idx);
                                var iban = converted ? string.Empty : row.Cells[0].Value.ToString();
                                var saleId = row.Cells[1].Value == null ? string.Empty : row.Cells[1].Value.ToString();
                                var amount = decimal.Parse(row.Cells[2].Value.ToString());
                                sum += amount;

                                var data = new SaleIdData(idx, iban, saleId, amount, RowType.Private);

                                multiSaleId.SaleIds.Add(data);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        PcPos.Amount = sum.ToString();

                        PcPos.SetMultiSaleId(multiSaleId);
                    }

                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtGovOrderId.Text = GetUnixTime();
                    }
                    if (!string.IsNullOrEmpty(txtGovOrderId.Text))
                        PcPos.SetOrderId(txtGovOrderId.Text);

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnGovernmentInquiriedIdentifiedSaleResult += PcPos_OnOrganizationInquiriedIdentifiedSaleResult;
                            btnPayBillInquiredSaleOrganization.Text = Resources.Cancel_Buy;
                            PcPos.AsyncGovernmentInquiriedIdentifiedTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnPayBillInquiredSaleOrganization.Text = Resources.Cancel_Buy;
                            PcPos_OnOrganizationInquiriedIdentifiedSaleResult(null, PcPos.SyncGovernmentInquiriedIdentifiedTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnGovernmentInquiriedIdentifiedSaleResult += PcPos_OnOrganizationInquiriedIdentifiedSaleResult;
                                PcPos.AsyncGovernmentInquiriedIdentifiedTransaction();
                                btnPayBillInquiredSaleOrganization.Text = Resources.Cancel_Buy;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnPayBillInquiredSaleOrganization.Text = Resources.Cancel_Buy;
                                PcPos_OnOrganizationInquiriedIdentifiedSaleResult(null, PcPos.SyncGovernmentInquiriedIdentifiedTransaction());
                            }
                        }
                        else
                        {
                            btnPayBillInquiredSaleOrganization.Text = Resources.Buy;
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                        }
                    }
                    else
                    {
                        btnPayBillInquiredSaleOrganization.Text = Resources.Buy;
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                }
                else if (btnPayBillInquiredSaleOrganization.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnPayBillInquiredSaleOrganization.Text = Resources.Buy;
                }
            }
            catch (Exception ex)
            {
                btnPayBillInquiredSaleOrganization.Text = Resources.Buy;
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPos_OnOrganizationInquiriedIdentifiedSaleResult(object sender, PosResult pPosResult)
        {
            Action<PosResult> fillResult = delegate (PosResult e)
            {
                txtPacketTypeInquiredSaleOrganization.Text = e.PacketType;
                txtResponseCodeInquiredSaleOrganization.Text = e.ResponseCode;
                txtCardNoInquiredSaleOrganization.Text = e.CardNo;
                txtProcessingCodeInquiredSaleOrganization.Text = e.ProcessingCode;
                txtTransactionNoInquiredSaleOrganization.Text = e.TransactionNo;
                txtTransactionTimeInquiredSaleOrganization.Text = e.TransactionTime;
                txtTransactionDateInquiredSaleOrganization.Text = e.TransactionDate;
                txtRRNInquiredSaleOrganization.Text = e.Rrn;
                txtApprovalCodeInquiredSaleOrganization.Text = e.ApprovalCode;
                txtTerminalInquiredSaleOrganization.Text = e.TerminalId;
                txtMerchantnoInquiredSaleOrganization.Text = e.MerchantId;
                txtOptinalFieldInquiredSaleOrganization.Text = e.OptionalField;
                txtPcPosStatusInquiredSaleOrganization.Text = e.PcPosStatus;
                txtAmountInquiredSaleOrganization.Text = e.Amount;
                txtGovOrderIdRes.Text = e.OrderId;
                btnPayBillInquiredSaleOrganization.Text = Resources.Buy;
                btnGetCardInfo.Text = Resources.GetCardInfo;

                if (e.PcPosStatusCode != (int)FrameExchangeResponse.SimultaneousRequestError)
                {
                    //PcPos = null;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosResult);
                    SaveDb(pPosResult);
                }));
            }
            else
            {
                fillResult(pPosResult);
                SaveDb(pPosResult);
            }
        }

        private void txtAddPosIp_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnAddPosIp_Click(sender, e);
            }
        }

        private void btnAddPosIp_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtAddPosIp.Text)) return;
            if (!CheckIpValid(txtAddPosIp.Text)) return;

            var mainNode = new TreeNode("Static POS IP")
            {
                Tag = new PosDevice()
                {
                    IpAddress = txtAddPosIp.Text,
                    Port = "8888"
                }
            };
            treeViewEx1.Nodes.Add(mainNode);
            mainNode.Nodes.Add(new TreeNode("MerchantId"));
            mainNode.Nodes.Add(new TreeNode("TerminalId"));
            mainNode.Nodes.Add(new TreeNode(txtAddPosIp.Text));
            mainNode.Nodes.Add(new TreeNode("8888"));

            if (treeViewEx1.Nodes.Count == 1)
                treeViewEx1.Nodes[0].Checked = true;

            txtAddPosIp.Clear();
        }

        private void btnReceiveLog_Click(object sender, EventArgs e)
        {
            try
            {
                var bus = new PcPosBusiness();
                //bus.RetryTimeOut = retryTimeOut;
                //bus.ResponseTimeOut = responseTimeout;

                var lastTransactionCount = (int)numLastTransaction.Value;
                var enumName = Enum.GetName(typeof(ReportTransactionType), cmbLastTransactionType.SelectedIndex);
                if (string.IsNullOrEmpty(enumName)) enumName = ReportTransactionType.Successful.ToString();
                var lastTransactionType = (ReportTransactionType)Enum.Parse(typeof(ReportTransactionType), enumName, true);
                bus.SetReportInfo(lastTransactionCount, lastTransactionType);

                var selectedIpDevice = CheckedDevices(treeViewEx1);

                if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                {
                    if (selectedIpDevice.Count == 0)
                    {
                        MessageBox.Show(Resources.Error_No_Device_Choosed);
                        return;
                    }
                    if (selectedIpDevice.Count > 1)
                    {
                        MessageBox.Show(Resources.Error_Device_Choose_Count);
                        return;
                    }
                    dataGridViewReport.DataSource = null;

                    bus.Ip = selectedIpDevice[0].IpAddress;
                    bus.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                    bus.ConnectionType = PcPosConnectionType.Lan;

                    //set result call back
                    if (radioButtonAsync.Checked)
                    {
                        bus.OnReportResult += Bus_OnReportResult;
                        bus.AsyncTransactionReportV2();
                    }
                    else if (radioButtonSync.Checked)
                    {
                        var logs = bus.SyncTransactionReportV2();
                        Bus_OnReportResult(null, logs);
                    }
                }
                else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                {
                    if (cmbSerialPort.SelectedItem != null)
                    {
                        bus.ComPortName = cmbSerialPort.SelectedItem.ToString();
                        bus.ConnectionType = PcPosConnectionType.Serial;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            bus.OnReportResult += Bus_OnReportResult;
                            bus.AsyncTransactionReportV2();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            var logs = bus.SyncTransactionReportV2();
                            Bus_OnReportResult(null, logs);
                        }
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                }
                else
                    MessageBox.Show(Resources.Error_Choose_Serial_Port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Bus_OnReportResult(object sender, PosReportResult e)
        {
            //StringBuilder sb = new StringBuilder();
            //e.Items.ForEach(i =>
            //{
            //    sb.AppendLine("TransactionType: " + i.TransactionType);
            //    sb.AppendLine("Amount: " + i.Amount);
            //    sb.AppendLine("TraceNumber: " + i.TransactionNo);
            //    sb.AppendLine("TransactionDate: " + i.TransactionDate);
            //    sb.AppendLine("TransactionTime: " + i.TransactionTime);
            //    sb.AppendLine("Pan: " + i.Pan);
            //    sb.AppendLine("Rrn: " + i.Rrn);
            //    sb.AppendLine("ResponseCode: " + i.ResponseCode);
            //    sb.AppendLine("---------------------------------------------");
            //});
            //MessageBox.Show(sb.ToString());

            if (InvokeRequired)
            {
                dataGridViewReport.BeginInvoke(new MethodInvoker(() =>
                {
                    dataGridViewReport.DataSource = e.Items;
                }));
            }
            else
            {
                dataGridViewReport.DataSource = e.Items;
            }
        }

        private void btnGetCardInfo_Click(object sender, EventArgs e)
        {
            txtCardInfo.Clear();
            try
            {
                if (btnGetCardInfo.Text == Resources.GetCardInfo)
                {
                    //PcPos = null;
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;
                    PcPos.Amount = txtPayAmount.Text;

                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtOrderId.Text = GetUnixTime();
                    }
                    PcPos.SetOrderId(txtOrderId.Text);

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            //PcPos.OnCardInfoResult += PcPos_OnCardInfoResult;
                            PcPos.AsyncCardInfo();
                            btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                            PcPos_OnCardInfoResult(null, PcPos.SyncCardInfo());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                //PcPos.OnCardInfoResult += PcPos_OnCardInfoResult;
                                PcPos.AsyncCardInfo();
                                btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                                PcPos_OnCardInfoResult(null, PcPos.SyncCardInfo());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnGetCardInfo.Text == Resources.CancelGetCardInfo)
                {
                    PcPos.AbortPcPosOperation();
                    btnGetCardInfo.Text = Resources.GetCardInfo;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPos_OnCardInfoResultAction(PosCardInfoResult e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.CardInfo))
                    PcPos.SetCardInfo(e.CardInfo);
            }
            catch (Exception ex)
            {
                txtCardInfo.Text = ex.Message;
            }

            txtCardInfo.Text = e.CardInfo;
            Terminal.Text = e.TerminalId;
            Merchant.Text = e.MerchantId;
            CardNo.Text = e.CardNo;
            PcPosStatus.Text = e.PcPosStatus;
            btnGetCardInfo.Text = Resources.GetCardInfo;

            PacketType.Text = e.PacketType;
            ResponseCode.Text = e.ResponseCode;
            Amount.Text = e.Amount;
            ProcessingCode.Text = e.ProcessingCode;
            TransactionNo.Text = e.TransactionNo;
            TransactionTime.Text = e.TransactionTime;
            transactionDate.Text = e.TransactionDate;
            RRN.Text = e.Rrn;
            ApprovalCode.Text = e.ApprovalCode;
            OptionalField.Text = e.OptionalField;
            txtSaleOrderId.Text = e.OrderId;

            if (chbAutoSale.Checked && !string.IsNullOrEmpty(e.CardInfo))
            {
                Timer t = new Timer();
                t.Tick += (s, ev) =>
                {
                    t.Stop();

                    btnSale_Click(null, e);
                };

                t.Interval = 300;
                t.Start();
            }
            //if (!string.IsNullOrEmpty(e.CardInfo))
            //    btnSale_Click(null, e);
        }

        private void PcPos_OnCardInfoResult(object sender, PosCardInfoResult e)
        {
            if (InvokeRequired)
            {
                txtCardInfo.BeginInvoke(new MethodInvoker(() =>
                {
                    PcPos_OnCardInfoResultAction(e);
                }));
            }
            else
            {
                PcPos_OnCardInfoResultAction(e);
            }
        }

        private void chbAutoGetCard_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                CreatePcPosObject();
                txtCardInfo.Clear();

                #region Clear PcPos Data
                PcPos.ClearAmount();
                PcPos.ClearBillInfo();
                PcPos.ClearCardInfo();
                PcPos.ClearMultiAccountData();
                PcPos.ClearMultiSaleId();
                PcPos.ClearOrderId();
                PcPos.ClearSaleId();
                #endregion

                if (chbAutoGetCard.Checked)
                {
                    treeViewEx1.Update();

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                        PcPos.Amount = txtPayAmount.Text;

                        //set result call back
                        if (chbAutoGetCard.Checked)
                        {
                            //PcPos.OnCardInfoResult += PcPos_OnCardInfoResult;

                            PcPos.StartGettingCardInfo();

                            btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                            btnAddPosIp.Enabled = false;
                        }
                        else
                        {
                            PcPos?.StopGettingCardInfo();
                            btnGetCardInfo.Text = Resources.GetCardInfo;
                            btnAddPosIp.Enabled = true;
                        }
                    }
                    else // Serial
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                            PcPos.Amount = txtPayAmount.Text;

                            //set result call back
                            if (chbAutoGetCard.Checked)
                            {
                                //PcPos.OnCardInfoResult += PcPos_OnCardInfoResult;

                                PcPos.StartGettingCardInfo();

                                btnGetCardInfo.Text = Resources.CancelGetCardInfo;
                                btnAddPosIp.Enabled = false;
                            }
                            else
                            {
                                PcPos?.StopGettingCardInfo();
                                btnGetCardInfo.Text = Resources.GetCardInfo;
                                btnAddPosIp.Enabled = true;
                            }
                        }
                        else
                        {
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                            return;
                        }
                    }
                }
                else
                {
                    PcPos.StopGettingCardInfo();
                }
            }
            catch (Exception ex)
            {
                chbAutoGetCard.Checked = !chbAutoGetCard.Checked;
                MessageBox.Show(ex.Message);
            }
        }

        private void chbAutoSale_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            PcPos?.Dispose();
        }

        private void btnConvertIban_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedIpDevice = CheckedDevices(treeViewEx1);

                string merchantId = selectedIpDevice[0].MerchantId;
                string terminalId = selectedIpDevice[0].TerminalId;

                if (!string.IsNullOrEmpty(txtIbanDevide.Text))
                {
                    var mmpdxs = new List<MultiAccountPosDividerEx>();
                    foreach (var s in txtIbanDevide.Text.Split(','))
                    {
                        var mmpdx = new MultiAccountPosDividerEx()
                        {
                            Iban = s.Split(':')[0].Replace(" ", ""),
                            Value = s.Split(':')[1].Replace(" ", "")
                        };
                        mmpdxs.Add(mmpdx);
                    }

                    CreatePcPosObject();
                    var mmpds = PcPos.ConvertIbans(terminalId, merchantId, mmpdxs);

                    StringBuilder sb = new StringBuilder();
                    foreach (var item in mmpds)
                    {
                        if (sb.Length > 0)
                            sb.Append(",");
                        sb.Append(item.Index);
                        sb.Append(":");
                        sb.Append(item.Value);
                    }
                    txtMultiAccount.Text = sb.ToString();
                }
            }
            catch (Exception ex)
            {
                txtMultiAccount.Text = ex.Message;
            }
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.Contains(tabPage6))
            {
                tabControl1.TabPages.Remove(tabPage6);
            }
            else
            {
                tabControl1.TabPages.Add(tabPage6);
                tabControl1.SelectedTab = tabPage6;
            }
        }

        private void btnTestSale_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnTestSale.Text == Resources.Buy)
                {
                    //CleanResult();
                    txtTestResponseCode.Text = "";
                    txtTestCardNo.Text = "";
                    txtTestProcessingCode.Text = "";
                    txtTestTransactionNo.Text = "";
                    txtTestTransactionTime.Text = "";
                    txtTestTransactionDate.Text = "";
                    txtTestSerialNo.Text = "";
                    txtTestAmountRes.Text = "";
                    txtTestRrn.Text = "";
                    txtTestApprovalCode.Text = "";
                    txtTestTerminal.Text = "";
                    txtTestMerchant.Text = "";
                    txtTestOptionalField.Text = "";
                    txtTestPcPosStatus.Text = "";


                    var testPcPos = new PcPosBusiness();

                    #region Clear PcPos Data
                    testPcPos.ClearAmount();
                    testPcPos.ClearBillInfo();
                    testPcPos.ClearCardInfo();
                    testPcPos.ClearMultiAccountData();
                    testPcPos.ClearMultiSaleId();
                    testPcPos.ClearOrderId();
                    testPcPos.ClearSaleId();
                    #endregion

                    //if (cmbDeviceType.Text.Equals("Magic", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //    var mData = new MagicData();
                    //    mData.Amount = txtPayAmount.Text;
                    //    mData.TerminalId = "001";
                    //    mData.MerchantId = "123456789";

                    //    PcPos.MagicData = mData;
                    //    PcPos.DeviceType = DeviceType.Magic;
                    //    PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                    //    if (radioButtonAsync.Checked)
                    //        PcPos.AsyncSaleTransaction();
                    //    else
                    //        PcPos.SyncSaleTransaction();
                    //    return;
                    //}
                    testPcPos.Amount = txtTestAmount.Text;
                    //testPcPos.RetryTimeOut = retryTimeOut;
                    //testPcPos.ResponseTimeOut = responseTimeout;

                    //PcPos.Field1 = txtTestField1.Text;
                    //PcPos.Field2 = txtTestField2.Text;
                    //PcPos.Field3 = txtTestField3.Text;
                    //PcPos.Field4 = txtTestField4.Text;
                    //PcPos.Field5 = txtTestField5.Text;
                    //PcPos.Field6 = txtTestField6.Text;
                    //PcPos.Field7 = txtTestField7.Text;
                    //PcPos.Field8 = txtTestField8.Text;

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        testPcPos.Ip = selectedIpDevice[0].IpAddress;
                        testPcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        testPcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            testPcPos.OnSaleResult += PcPosTestSaleResult;
                            testPcPos.AsyncSaleTransaction();
                            btnTestSale.Text = Resources.Cancel_Buy;
                            DisableButtons(btnTestSale);
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnTestSale.Text = Resources.Cancel_Buy;
                            DisableButtons(btnTestSale);
                            PcPosTestSaleResult(null, testPcPos.SyncSaleTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            testPcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            testPcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                testPcPos.OnSaleResult += PcPosTestSaleResult;
                                testPcPos.AsyncSaleTransaction();

                                btnTestSale.Text = Resources.Cancel_Buy;
                                DisableButtons(btnTestSale);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnTestSale.Text = Resources.Cancel_Buy;
                                DisableButtons(btnTestSale);
                                var res = testPcPos.SyncSaleTransaction();
                                PcPosTestSaleResult(null, res);
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnTestSale.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnTestSale.Text = Resources.Buy;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPosTestSaleResult(object sender, PosResult pPosResult)
        {
            Action<PosResult> fillResult = delegate (PosResult e)
            {
                txtTestPacketType.Text = e.PacketType;
                txtTestResponseCode.Text = e.ResponseCode;
                txtTestAmount.Text = e.Amount;
                txtTestCardNo.Text = e.CardNo;
                txtTestProcessingCode.Text = e.ProcessingCode;
                txtTestTransactionNo.Text = e.TransactionNo;
                txtTestTransactionTime.Text = e.TransactionTime;
                txtTestTransactionDate.Text = e.TransactionDate;
                txtTestRrn.Text = e.Rrn;
                txtTestApprovalCode.Text = e.ApprovalCode;
                txtTestTerminal.Text = e.TerminalId;
                txtTestMerchant.Text = e.MerchantId;
                txtTestOptionalField.Text = e.OptionalField;
                txtTestPcPosStatus.Text = e.PcPosStatus;

                ResetTestTabButton();

                if (e.PcPosStatusCode != (int)FrameExchangeResponse.SimultaneousRequestError)
                {
                    //PcPos = null;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosResult);
                    SaveDb(pPosResult);
                }));
            }
            else
            {
                fillResult(pPosResult);
                SaveDb(pPosResult);
            }
        }

        private void ResetTestTabButton()
        {
            btnTestSale.Text = Resources.Buy;
            btnTestBillPayment.Text = Resources.Bill_Payment;
            btnTestInquiredPayment.Text = Resources.Inquired_Payment;
            btnTestGovernmentPayment.Text = Resources.Government_Payment;

            EnableButtons();
        }

        private void EnableButtons()
        {
            btnTestSale.Enabled = true;
            btnTestBillPayment.Enabled = true;
            btnTestInquiredPayment.Enabled = true;
            btnTestGovernmentPayment.Enabled = true;
            btnTestCardInfo.Enabled = true;
        }

        private void DisableButtons(Button pButton)
        {
            btnTestSale.Enabled = false;
            btnTestBillPayment.Enabled = false;
            btnTestInquiredPayment.Enabled = false;
            btnTestGovernmentPayment.Enabled = false;
            btnTestCardInfo.Enabled = false;

            pButton.Enabled = true;
        }

        private void btnTestBillPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnTestBillPayment.Text == Resources.Bill_Payment)
                {
                    //CleanResult();
                    txtTestResponseCode.Text = "";
                    txtTestCardNo.Text = "";
                    txtTestProcessingCode.Text = "";
                    txtTestTransactionNo.Text = "";
                    txtTestTransactionTime.Text = "";
                    txtTestTransactionDate.Text = "";
                    txtTestSerialNo.Text = "";
                    txtTestAmountRes.Text = "";
                    txtTestRrn.Text = "";
                    txtTestApprovalCode.Text = "";
                    txtTestTerminal.Text = "";
                    txtTestMerchant.Text = "";
                    txtTestOptionalField.Text = "";
                    txtTestPcPosStatus.Text = "";

                    var testPcPos = new PcPosBusiness();

                    #region Clear PcPos Data
                    testPcPos.ClearAmount();
                    testPcPos.ClearBillInfo();
                    testPcPos.ClearCardInfo();
                    testPcPos.ClearMultiAccountData();
                    testPcPos.ClearMultiSaleId();
                    testPcPos.ClearOrderId();
                    testPcPos.ClearSaleId();
                    #endregion

                    testPcPos.Amount = txtTestAmount.Text;
                    //testPcPos.RetryTimeOut = retryTimeOut;
                    //testPcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        testPcPos.Ip = selectedIpDevice[0].IpAddress;
                        testPcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        testPcPos.ConnectionType = PcPosConnectionType.Lan;
                        //set result call back

                        if (radioButtonAsync.Checked)
                        {
                            testPcPos.OnBillPaymentResult += PcPosTestSaleResult;
                            btnTestBillPayment.Text = Resources.Cancel_Bill_Payment;
                            DisableButtons(btnTestBillPayment);
                            testPcPos.AsyncBillPaymentTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnTestBillPayment.Text = Resources.Cancel_Bill_Payment;
                            DisableButtons(btnTestBillPayment);
                            PcPosTestSaleResult(null, testPcPos.SyncBillPaymentTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            testPcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            testPcPos.ConnectionType = PcPosConnectionType.Serial;
                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                testPcPos.OnBillPaymentResult += PcPosTestSaleResult;
                                testPcPos.AsyncBillPaymentTransaction();
                                btnTestBillPayment.Text = Resources.Cancel_Bill_Payment;
                                DisableButtons(btnTestBillPayment);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnTestBillPayment.Text = Resources.Cancel_Bill_Payment;
                                DisableButtons(btnTestBillPayment);
                                PcPosTestSaleResult(null, testPcPos.SyncBillPaymentTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                }
                else if (btnTestBillPayment.Text == Resources.Cancel_Bill_Payment)
                {
                    PcPos.AbortPcPosOperation();
                    btnTestBillPayment.Text = Resources.Bill_Payment;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnTestInquiredPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnTestInquiredPayment.Text == Resources.Inquired_Payment)
                {
                    CleanResult();
                    var testPcPos = new PcPosBusiness();

                    #region Clear PcPos Data
                    testPcPos.ClearAmount();
                    testPcPos.ClearBillInfo();
                    testPcPos.ClearCardInfo();
                    testPcPos.ClearMultiAccountData();
                    testPcPos.ClearMultiSaleId();
                    testPcPos.ClearOrderId();
                    testPcPos.ClearSaleId();
                    #endregion

                    //PcPos.Amount = txtTestAmount.Text;
                    //testPcPos.RetryTimeOut = retryTimeOut;
                    //testPcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        testPcPos.Ip = selectedIpDevice[0].IpAddress;
                        testPcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        testPcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            testPcPos.OnInquiriedIdentifiedSaleResult += PcPosTestSaleResult;
                            btnTestInquiredPayment.Text = Resources.Cancel_Pay;
                            DisableButtons(btnTestInquiredPayment);
                            testPcPos.AsyncInquiriedIdentifiedTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnTestInquiredPayment.Text = Resources.Cancel_Pay;
                            DisableButtons(btnTestInquiredPayment);
                            PcPosTestSaleResult(null, testPcPos.SyncInquiriedIdentifiedTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            testPcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            testPcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                testPcPos.OnInquiriedIdentifiedSaleResult += PcPosTestSaleResult;
                                testPcPos.AsyncInquiriedIdentifiedTransaction();
                                btnTestInquiredPayment.Text = Resources.Cancel_Pay;
                                DisableButtons(btnTestInquiredPayment);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnTestInquiredPayment.Text = Resources.Cancel_Pay;
                                DisableButtons(btnTestInquiredPayment);
                                PcPosTestSaleResult(null, testPcPos.SyncInquiriedIdentifiedTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnTestInquiredPayment.Text == Resources.Cancel_Pay)
                {
                    PcPos.AbortPcPosOperation();
                    btnTestInquiredPayment.Text = Resources.Inquired_Payment;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnTestGovernmentPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnTestGovernmentPayment.Text == Resources.Government_Payment)
                {
                    CleanResult();
                    var testPcPos = new PcPosBusiness();

                    #region Clear PcPos Data
                    testPcPos.ClearAmount();
                    testPcPos.ClearBillInfo();
                    testPcPos.ClearCardInfo();
                    testPcPos.ClearMultiAccountData();
                    testPcPos.ClearMultiSaleId();
                    testPcPos.ClearOrderId();
                    testPcPos.ClearSaleId();
                    #endregion

                    if (!string.IsNullOrEmpty(txtTestAmount.Text))
                        testPcPos.Amount = txtTestAmount.Text;
                    //testPcPos.RetryTimeOut = retryTimeOut;
                    //testPcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        testPcPos.Ip = selectedIpDevice[0].IpAddress;
                        testPcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        testPcPos.ConnectionType = PcPosConnectionType.Lan;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            testPcPos.OnGovernmentInquiriedIdentifiedSaleResult += PcPosTestSaleResult;
                            btnTestGovernmentPayment.Text = Resources.Cancel_Pay;
                            DisableButtons(btnTestGovernmentPayment);
                            testPcPos.AsyncGovernmentInquiriedIdentifiedTransaction();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnTestGovernmentPayment.Text = Resources.Cancel_Pay;
                            DisableButtons(btnTestInquiredPayment);
                            PcPosTestSaleResult(null, testPcPos.SyncGovernmentInquiriedIdentifiedTransaction());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            testPcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                testPcPos.OnGovernmentInquiriedIdentifiedSaleResult += PcPosTestSaleResult;
                                testPcPos.AsyncGovernmentInquiriedIdentifiedTransaction();
                                btnTestGovernmentPayment.Text = Resources.Cancel_Pay;
                                DisableButtons(btnTestGovernmentPayment);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnTestGovernmentPayment.Text = Resources.Cancel_Pay;
                                DisableButtons(btnTestGovernmentPayment);
                                PcPosTestSaleResult(null, testPcPos.SyncGovernmentInquiriedIdentifiedTransaction());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                }
                else if (btnTestGovernmentPayment.Text == Resources.Cancel_Pay)
                {
                    PcPos.AbortPcPosOperation();
                    btnTestGovernmentPayment.Text = Resources.Government_Payment;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbTestMultiAccount_SelectedIndexChanged(object sender, EventArgs e)
        {
            var pcPos = new PcPosBusiness();
            var listTimeOutDic = new List<MultiAccountPosDivider>();

            var percenrMode = "1:20,2:30,3:10,4:6,5:4,6:6,7:6,8:6,9:6,10:6";
            var amountMode = new StringBuilder();
            amountMode.Append("1:20");
            amountMode.Append(",2:30");
            amountMode.Append(",3:10");
            amountMode.Append(",4:10");
            amountMode.Append(",5:10");
            amountMode.Append(",6:10");
            amountMode.Append(",7:10");
            amountMode.Append(",8:10");
            amountMode.Append(",9:10");
            amountMode.Append(",10:");
            var amount = int.Parse(txtTestAmount.Text);
            amountMode.Append(amount - 120);

            if (cmbTestMultiAccount.SelectedItem.ToString().Contains("1") || cmbTestMultiAccount.SelectedItem.ToString().Contains("3"))
            {
                foreach (var s in percenrMode.Split(','))
                {
                    var mmpd = new MultiAccountPosDivider()
                    {
                        Index = int.Parse(s.Split(':')[0]),
                        Value = s.Split(':')[1]
                    };
                    listTimeOutDic.Add(mmpd);
                }
            }
            else
            {
                foreach (var s in amountMode.ToString().Split(','))
                {
                    var mmpd = new MultiAccountPosDivider()
                    {
                        Index = int.Parse(s.Split(':')[0]),
                        Value = s.Split(':')[1]
                    };
                    listTimeOutDic.Add(mmpd);
                }
            }

            int multiAccountErrorCode = 0;
            if (cmbTestMultiAccount.SelectedItem.ToString().Contains("1")) // درصدی قدیم
            {
                if (!pcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentOld, out multiAccountErrorCode))
                {
                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                    return;
                }
            }
            else if (cmbTestMultiAccount.SelectedItem.ToString().Contains("2")) // مبلغی قدیم
            {
                if (!pcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountOld, out multiAccountErrorCode))
                {
                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                    return;
                }
            }
            else if (cmbTestMultiAccount.SelectedItem.ToString().Contains("3")) // درصدی جدید
            {
                if (!pcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentNew, out multiAccountErrorCode))
                {
                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                    return;
                }
            }
            else if (cmbTestMultiAccount.SelectedItem.ToString().Contains("4")) // مبلغی جدید
            {
                if (!pcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountNew, out multiAccountErrorCode))
                {
                    MessageBox.Show("Invalid input for Multi-Merchant data.");
                    return;
                }
            }

            //txtTestField1.Text = !string.IsNullOrEmpty(pcPos.set.Field1) ? pcPos.Field1.Remove(0, 3) : pcPos.Field1;
        }

        private void cmbTestSaleId_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbTestSaleId.SelectedIndex)
            {
                case 0:
                    txtTestField2.Clear();
                    break;
                case 1:
                    txtTestField2.Text = "1021100002200001211000000000000";
                    txtTestAmount.Text = "110000";
                    break;
                case 2:
                    txtTestField2.Text = "362110000220000122100";
                    break;
                case 3:
                    txtTestField2.Text = "139522000000014335";
                    break;
                case 4:
                    txtTestField2.Text = "6039628301226";
                    break;
                default:
                    break;
            }
        }

        private void cmbTestBillInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtTestAmount.Text = "312000";
            txtTestField3.Text = string.Format("{0},{1}", "6546516210022", "31210462");
        }

        private void btnTestCardInfo_Click(object sender, EventArgs e)
        {
            txtCardInfo.Clear();
            try
            {
                if (btnTestCardInfo.Text == Resources.Test_CardInfo)
                {
                    //PcPos = null;
                    var testPcPos = new PcPosBusiness();

                    #region Clear PcPos Data
                    testPcPos.ClearAmount();
                    testPcPos.ClearBillInfo();
                    testPcPos.ClearCardInfo();
                    testPcPos.ClearMultiAccountData();
                    testPcPos.ClearMultiSaleId();
                    testPcPos.ClearOrderId();
                    testPcPos.ClearSaleId();
                    #endregion

                    //testPcPos.RetryTimeOut = retryTimeOut;
                    //testPcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        testPcPos.Ip = selectedIpDevice[0].IpAddress;
                        testPcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        testPcPos.ConnectionType = PcPosConnectionType.Lan;
                        testPcPos.Amount = txtPayAmount.Text;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            testPcPos.OnCardInfoResult += PcPosTest_OnCardInfoResult;
                            testPcPos.AsyncCardInfo();
                            btnTestCardInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnTestCardInfo);
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnTestCardInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnTestCardInfo);
                            PcPosTest_OnCardInfoResult(null, testPcPos.SyncCardInfo());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            testPcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            testPcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                testPcPos.OnCardInfoResult += PcPosTest_OnCardInfoResult;
                                testPcPos.AsyncCardInfo();
                                btnTestCardInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnTestCardInfo);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnTestCardInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnTestCardInfo);
                                PcPosTest_OnCardInfoResult(null, testPcPos.SyncCardInfo());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnTestCardInfo.Text == Resources.Cancel_Transaction)
                {
                    PcPos.AbortPcPosOperation();
                    btnTestCardInfo.Text = Resources.Test_CardInfo;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPosTest_OnCardInfoResult(object sender, PosCardInfoResult e)
        {
            if (txtTestField4.InvokeRequired)
            {
                txtTestField4.BeginInvoke(new MethodInvoker(() =>
                {
                    txtTestField4.Text = e.CardInfo;
                }));
            }
            else
            {
                txtTestField4.Text = e.CardInfo;
            }
        }

        private void cmbxDivideType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var percenrMode = "1:20,2:30,3:10,4:6,5:4,6:6,7:6,8:6,9:6,10:6";
            var amountMode = new StringBuilder();
            amountMode.Append("1:20");
            amountMode.Append(",2:30");
            amountMode.Append(",3:10");
            amountMode.Append(",4:10");
            amountMode.Append(",5:10");
            amountMode.Append(",6:10");
            amountMode.Append(",7:10");
            amountMode.Append(",8:10");
            amountMode.Append(",9:10");
            amountMode.Append(",10:");
            var amount = int.Parse(txtPayAmount.Text);
            amountMode.Append(amount - 120);

            if (cmbxDivideType.SelectedItem.ToString().Contains("1") || cmbxDivideType.SelectedItem.ToString().Contains("3"))
            {
                txtMultiAccount.Text = percenrMode;
            }
            else
            {
                txtMultiAccount.Text = amountMode.ToString();
            }
        }

        private void btnCheckDeviceInfo_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnCheckDeviceInfo.Text == Resources.DeviceInfo)
                {
                    DeviceInfoFlag = btnCheckDeviceInfo.Name;
                    btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = false;

                    Terminal.Clear();
                    Merchant.Clear();
                    OptionalField.Clear();

                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                        PcPos.Amount = txtPayAmount.Text;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            PcPos.AsyncGetPosDeviceInfo();
                            btnCheckDeviceInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnCheckDeviceInfo);
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnCheckDeviceInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnCheckDeviceInfo);
                            PcPos_OnPosDeviceInfoResult(null, PcPos.SyncGetPosDeviceInfo());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                PcPos.AsyncGetPosDeviceInfo();
                                btnCheckDeviceInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnCheckDeviceInfo);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnCheckDeviceInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnCheckDeviceInfo);
                                PcPos_OnPosDeviceInfoResult(null, PcPos.SyncGetPosDeviceInfo());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnCheckDeviceInfo.Text == Resources.Cancel_Transaction)
                {
                    PcPos.AbortPcPosOperation();
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                    btnCheckDeviceInfo.Text = Resources.DeviceInfo;
                    EnableButtons();
                }
            }
            catch (Exception ex)
            {
                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPos_OnPosDeviceInfoResult(object sender, PosDevice pPosDevice)
        {
            Action<PosDevice> fillResult = delegate (PosDevice e)
            {
                if (DeviceInfoFlag == btnCheckDeviceInfo.Name)
                {
                    Terminal.Text = e.TerminalId;
                    Merchant.Text = e.MerchantId;
                    OptionalField.Text = string.Format("{0}, {1}, {2}:{3}", e.Header, e.MerchantName, e.IpAddress, e.Port);

                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                    btnCheckDeviceInfo.Text = Resources.DeviceInfo;
                }
                else
                {
                    txtDeviceTerminalId.Text = e.TerminalId;
                    txtDeviceMerchantId.Text = e.MerchantId;
                    txtDeviceMerchantName.Text = e.MerchantName;
                    txtDeviceHeader.Text = e.Header;
                    txtDeviceIp.Text = e.IpAddress;
                    txtDevicePort.Text = e.Port;
                    txtDeviceBrand.Text = e.BrandModel;

                    btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                    btnPosDesiveInfo.Text = Resources.DeviceInfo;
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(pPosDevice);
                }));
            }
            else
            {
                fillResult(pPosDevice);
            }
        }

        private void cmbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDeviceType.SelectedText.Equals("Magic", StringComparison.InvariantCultureIgnoreCase) ||
                cmbDeviceType.Text.Equals("Magic", StringComparison.InvariantCultureIgnoreCase))
            {
                lblPayMerchantId.Visible = true;
                lblPayTerminalId.Visible = true;
                txtPayTerminalId.Visible = true;
                txtPayMerchantId.Visible = true;
                btnMagicInquiry.Visible = true;
            }
            else
            {
                lblPayMerchantId.Visible = false;
                lblPayTerminalId.Visible = false;
                txtPayTerminalId.Visible = false;
                txtPayMerchantId.Visible = false;
                btnMagicInquiry.Visible = false;
            }
        }

        private void btnRefreshSerialPort_Click(object sender, EventArgs e)
        {
            try
            {
                cmbSerialPort.Items.Clear();
                var objArr = new List<object>(PcPosBusiness.GetAvailableSerialPorts().ToArray());
                cmbSerialPort.Items.AddRange(objArr.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void lblSalePayId_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtPayId.Clear();
        }

        private void lblSaleMultiAccountData_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtMultiAccount.Clear();
        }

        private void btnCommodityBasket_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnCommodityBasket.Text == Resources.Commodity_Basket)
                {
                    btnCheckDeviceInfo.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = false;
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //set transaction id
                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtOrderId.Text = GetUnixTime();
                    }
                    if (!string.IsNullOrEmpty(txtOrderId.Text))
                        PcPos.SetOrderId(txtOrderId.Text);
                    PcPos.SerialNo = txtPayPosSerialNo.Text;

                    PcPos.Amount = txtPayAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                        }
                        else
                        {
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                        return;
                    }

                    #region Multi-Merchant Data

                    if (!string.IsNullOrEmpty(txtMultiAccount.Text))
                    {
                        var listTimeOutDic = new List<MultiAccountPosDivider>();
                        foreach (var s in txtMultiAccount.Text.Split(','))
                        {
                            var mmpd = new MultiAccountPosDivider()
                            {
                                Index = int.Parse(s.Split(':')[0]),
                                Value = s.Split(':')[1]
                            };
                            listTimeOutDic.Add(mmpd);
                        }

                        int multiAccountErrorCode = 0;
                        if (cmbxDivideType.SelectedItem.ToString().Contains("1")) // درصدی قدیم
                        {
                            //PcPos.SetMultiAccountsData(new List<string>(txtMultiAccount.Text.Split(new char[] { ',' })), MultiAccountMode.Percent);
                            if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentOld, out multiAccountErrorCode))
                            {
                                MessageBox.Show("Invalid input for Multi-Merchant data.");
                                return;
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("2")) // مبلغی قدیم
                        {
                            //PcPos.SetMultiAccountsData(new List<string>(txtMultiAccount.Text.Split(new char[] { ',' })), MultiAccountMode.Amoumt);
                            if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountOld, out multiAccountErrorCode))
                            {
                                MessageBox.Show("Invalid input for Multi-Merchant data.");
                                return;
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("3")) // درصدی جدید
                        {
                            if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.PercentNew, out multiAccountErrorCode))
                            {
                                MessageBox.Show("Invalid input for Multi-Merchant data.");
                                return;
                            }
                        }
                        else if (cmbxDivideType.SelectedItem.ToString().Contains("4")) // مبلغی جدید
                        {
                            if (!PcPos.SetMultiAccountData(listTimeOutDic, MultiAccountMode.AmountNew, out multiAccountErrorCode))
                            {
                                MessageBox.Show("Invalid input for Multi-Merchant data.");
                                return;
                            }
                        }
                    }

                    #endregion

                    //set result call back
                    if (radioButtonAsync.Checked)
                    {
                        //PcPos.OnSaleResult += PcPosSaleResult;
                        PcPos.AsyncCommodityBasket();
                        btnCommodityBasket.Text = Resources.Cancel_Buy;
                    }
                    else if (radioButtonSync.Checked)
                    {
                        btnCommodityBasket.Text = Resources.Cancel_Buy;
                        PcPosSaleResult(null, PcPos.SyncCommodityBasket());
                    }
                }
                else if (btnCommodityBasket.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                    btnCommodityBasket.Text = Resources.Commodity_Basket;
                }
            }
            catch (Exception ex)
            {
                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void btnFoodSafety_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnFoodSafety.Text == Resources.Food_Safety)
                {
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnSale.Enabled = false;
                    CleanResult();
                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //set transaction id
                    if (checkBoxCreateRandomOrderId.Checked)
                    {
                        txtOrderId.Text = GetUnixTime();
                    }
                    if (!string.IsNullOrEmpty(txtOrderId.Text))
                        PcPos.SetOrderId(txtOrderId.Text);
                    PcPos.SerialNo = txtPayPosSerialNo.Text;

                    PcPos.Amount = txtPayAmount.Text;
                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                            PcPos.ConnectionType = PcPosConnectionType.Serial;
                        }
                        else
                        {
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                        return;
                    }

                    //set result call back
                    if (radioButtonAsync.Checked)
                    {
                        //PcPos.OnSaleResult += PcPosSaleResult;
                        PcPos.AsyncFoodSafety();
                        btnFoodSafety.Text = Resources.Cancel_Buy;
                    }
                    else if (radioButtonSync.Checked)
                    {
                        btnFoodSafety.Text = Resources.Cancel_Buy;
                        PcPosSaleResult(null, PcPos.SyncFoodSafety());
                    }
                }
                else if (btnFoodSafety.Text == Resources.Cancel_Buy)
                {
                    PcPos.AbortPcPosOperation();
                    btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                    btnFoodSafety.Text = Resources.Food_Safety;
                }
            }
            catch (Exception ex)
            {
                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = btnSale.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbConnectionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbConnectionType.SelectedItem.ToString().Equals("Lan/Ethernet", StringComparison.InvariantCultureIgnoreCase))
            {
                cmbSerialPort.SelectedItem = null;
                btnRefreshSerialPort.Enabled = cmbSerialPort.Enabled = false;
                btnConfigSerial.Visible = false;

                btnSearchPos.Enabled = true;
                txtAddPosIp.Enabled = true;
                btnAddPosIp.Enabled = true;
                cmbSearchType.Enabled = true;
                txtSearchValue.Enabled = true;
                treeViewEx1.Enabled = true;
            }
            else
            {
                btnRefreshSerialPort.Enabled = cmbSerialPort.Enabled = true;
                btnConfigSerial.Visible = true;

                btnSearchPos.Enabled = false;
                txtAddPosIp.Enabled = false;
                btnAddPosIp.Enabled = false;
                cmbSearchType.Enabled = false;
                txtSearchValue.Enabled = false;
                treeViewEx1.Enabled = false;
            }
        }

        private void label18_Click(object sender, EventArgs e)
        {
            txtOrderId.Clear();
        }

        private void radioGovSaleId_CheckedChanged(object sender, EventArgs e)
        {
            if (radioOneGovSaleId.Checked)
            {
                txtSaleIdInquiredSaleOrganization.Enabled = true;
                txtSaleAmountInquiredSaleOrganization.Enabled = true;

                dataGridViewMultiSaleId.Enabled = false;
                dataGridViewMultiSaleId.ReadOnly = true;
            }
            else
            {
                txtSaleIdInquiredSaleOrganization.Enabled = false;
                txtSaleAmountInquiredSaleOrganization.Enabled = false;

                dataGridViewMultiSaleId.Enabled = true;
                dataGridViewMultiSaleId.ReadOnly = false;
            }
        }

        private void dataGridViewMultiSaleId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                for (int i = 0; i < dataGridViewMultiSaleId.SelectedCells.Count; i++)
                {
                    dataGridViewMultiSaleId.SelectedCells[i].Value = "";
                }
            }

        }

        private void btnPosDesiveInfo_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnPosDesiveInfo.Text == Resources.DeviceInfo)
                {
                    DeviceInfoFlag = btnPosDesiveInfo.Name;
                    btnAccountList.Enabled = false;

                    txtDeviceTerminalId.Clear();
                    txtDeviceMerchantId.Clear();
                    txtDeviceMerchantName.Clear();
                    txtDeviceHeader.Clear();
                    txtDeviceIp.Clear();
                    txtDevicePort.Clear();

                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                        PcPos.Amount = txtPayAmount.Text;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            PcPos.AsyncGetPosDeviceInfo();
                            btnPosDesiveInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnPosDesiveInfo);
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnPosDesiveInfo.Text = Resources.Cancel_Transaction;
                            DisableButtons(btnPosDesiveInfo);
                            PcPos_OnPosDeviceInfoResult(null, PcPos.SyncGetPosDeviceInfo());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                PcPos.AsyncGetPosDeviceInfo();
                                btnPosDesiveInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnPosDesiveInfo);
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnPosDesiveInfo.Text = Resources.Cancel_Transaction;
                                DisableButtons(btnPosDesiveInfo);
                                PcPos_OnPosDeviceInfoResult(null, PcPos.SyncGetPosDeviceInfo());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnPosDesiveInfo.Text == Resources.Cancel_Transaction)
                {
                    PcPos.AbortPcPosOperation();
                    btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                    btnPosDesiveInfo.Text = Resources.DeviceInfo;
                }
            }
            catch (Exception ex)
            {
                btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void PcPos_OnAccountInfoResult(object sender, PosAccount e)
        {
            Action<PosAccount> fillResult = delegate (PosAccount acc)
            {
                dataGridViewAccountList.DataSource = acc.Accounts;

                btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                btnAccountList.Text = Resources.AccountList;
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    fillResult(e);
                }));
            }
            else
            {
                fillResult(e);
            }
        }

        private void btnAccountList_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnAccountList.Text == Resources.AccountList)
                {
                    btnPosDesiveInfo.Enabled = false;

                    dataGridViewAccountList.DataSource = null;

                    CreatePcPosObject();

                    #region Clear PcPos Data
                    PcPos.ClearAmount();
                    PcPos.ClearBillInfo();
                    PcPos.ClearCardInfo();
                    PcPos.ClearMultiAccountData();
                    PcPos.ClearMultiSaleId();
                    PcPos.ClearOrderId();
                    PcPos.ClearSaleId();
                    #endregion

                    //PcPos.RetryTimeOut = retryTimeOut;
                    //PcPos.ResponseTimeOut = responseTimeout;

                    treeViewEx1.Update();

                    var selectedIpDevice = CheckedDevices(treeViewEx1);
                    if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                    {
                        if (selectedIpDevice.Count == 0)
                        {
                            MessageBox.Show(Resources.Error_No_Device_Choosed);
                            return;
                        }
                        if (selectedIpDevice.Count > 1)
                        {
                            MessageBox.Show(Resources.Error_Device_Choose_Count);
                            return;
                        }
                        PcPos.Ip = selectedIpDevice[0].IpAddress;
                        PcPos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                        PcPos.ConnectionType = PcPosConnectionType.Lan;
                        PcPos.Amount = txtPayAmount.Text;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            PcPos.AsyncGetAccounts();
                            btnAccountList.Text = Resources.Cancel_Transaction;
                        }
                        else if (radioButtonSync.Checked)
                        {
                            btnAccountList.Text = Resources.Cancel_Transaction;
                            PcPos_OnAccountInfoResult(null, PcPos.SyncGetAccounts());
                        }
                    }
                    else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                    {
                        if (cmbSerialPort.SelectedItem != null)
                        {
                            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

                            PcPos.ConnectionType = PcPosConnectionType.Serial;

                            //set result call back
                            if (radioButtonAsync.Checked)
                            {
                                PcPos.AsyncGetAccounts();
                                btnAccountList.Text = Resources.Cancel_Transaction;
                            }
                            else if (radioButtonSync.Checked)
                            {
                                btnAccountList.Text = Resources.Cancel_Transaction;
                                PcPos_OnAccountInfoResult(null, PcPos.SyncGetAccounts());
                            }
                        }
                        else
                            MessageBox.Show(Resources.Error_Choose_Serial_Port);
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);

                }
                else if (btnAccountList.Text == Resources.Cancel_Transaction)
                {
                    PcPos.AbortPcPosOperation();
                    btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                    btnAccountList.Text = Resources.AccountList;
                }
            }
            catch (Exception ex)
            {
                btnPosDesiveInfo.Enabled = btnAccountList.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                var bus = new PcPosBusiness();
                //bus.RetryTimeOut = retryTimeOut;
                //bus.ResponseTimeOut = responseTimeout;

                bus.SetSearchParam(new Dictionary<SearchParam, string>() { { SearchParam.OrderId, txtSearchOrderId.Text } });
                //bus.LastTransactionCount = numLastTransaction.Value.ToString("00");
                //var enumName = Enum.GetName(typeof(LogTransactionType), cmbLastTransactionType.SelectedIndex);
                //if (string.IsNullOrEmpty(enumName)) enumName = LogTransactionType.Successful.ToString();
                //bus.LastTransactionType = (LogTransactionType)Enum.Parse(typeof(LogTransactionType), enumName, true);

                var selectedIpDevice = CheckedDevices(treeViewEx1);

                if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                {
                    if (selectedIpDevice.Count == 0)
                    {
                        MessageBox.Show(Resources.Error_No_Device_Choosed);
                        return;
                    }
                    if (selectedIpDevice.Count > 1)
                    {
                        MessageBox.Show(Resources.Error_Device_Choose_Count);
                        return;
                    }
                    dataGridViewReport.DataSource = null;

                    bus.Ip = selectedIpDevice[0].IpAddress;
                    bus.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                    bus.ConnectionType = PcPosConnectionType.Lan;

                    //set result call back
                    if (radioButtonAsync.Checked)
                    {
                        bus.OnReportResult += Bus_OnReportResult;
                        bus.AsyncSearch();
                    }
                    else if (radioButtonSync.Checked)
                    {
                        var logs = bus.SyncSearch();
                        Bus_OnReportResult(null, logs);
                    }
                }
                else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                {
                    if (cmbSerialPort.SelectedItem != null)
                    {
                        bus.ComPortName = cmbSerialPort.SelectedItem.ToString();
                        bus.ConnectionType = PcPosConnectionType.Serial;

                        //set result call back
                        if (radioButtonAsync.Checked)
                        {
                            bus.OnReportResult += Bus_OnReportResult;
                            bus.AsyncSearch();
                        }
                        else if (radioButtonSync.Checked)
                        {
                            var logs = bus.SyncSearch();
                            Bus_OnReportResult(null, logs);
                        }
                    }
                    else
                        MessageBox.Show(Resources.Error_Choose_Serial_Port);
                }
                else
                    MessageBox.Show(Resources.Error_Choose_Serial_Port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnConfigSerial_Click(object sender, EventArgs e)
        {
            SerialPortConfig spc = new SerialPortConfig(cmbSerialPort.SelectedItem.ToString());

            int baudRate = 0;
            SadadStopBits stopBits = SadadStopBits.One;
            PcPos.GetSerialPortConfig(out baudRate, out stopBits);

            if (baudRate == 0)
            {
                var config = GetSettings().FindAll(c => c.SerialPort.Equals(cmbSerialPort.SelectedItem.ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (config == null || config.Count == 0)
                {
                    config = GetSettings().FindAll(c => c.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase));
                }

                if (config != null && config.Count > 0)
                {
                    baudRate = int.Parse(config[0].BaudRate);
                    stopBits = config[0].StopBits.Equals("1", StringComparison.InvariantCultureIgnoreCase) ? SadadStopBits.One : SadadStopBits.Two;
                    PcPos.SetSerialPortConfig(baudRate, stopBits);
                }
            }

            spc.baudRate.SelectedItem = baudRate.ToString();
            spc.stopBits.SelectedItem = stopBits == SadadStopBits.One ? "1" : "2";

            if (spc.ShowDialog() == DialogResult.OK)
            {
                var br = int.Parse(spc.baudRate.SelectedItem.ToString());
                var sb = spc.stopBits.SelectedItem.ToString() == "1" ? SadadStopBits.One : SadadStopBits.Two;
                PcPos.SetSerialPortConfig(br, sb);

                SaveSettings(br, sb);
            }
        }

        private void cmbSerialPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSerialPort.SelectedItem != null)
            {
                var config = GetSettings().FindAll(c => c.SerialPort.Equals(cmbSerialPort.SelectedItem.ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (config != null && config.Count > 0)
                {
                    int baudRate = int.Parse(config[0].BaudRate);
                    SadadStopBits stopBits = config[0].StopBits.Equals("1", StringComparison.InvariantCultureIgnoreCase) ? SadadStopBits.One : SadadStopBits.Two;
                    PcPos.SetSerialPortConfig(baudRate, stopBits);
                }
            }
        }

        private void cmbSerialPort_MouseHover(object sender, EventArgs e)
        {
            try
            {
                if (cmbSerialPort.SelectedItem != null || cmbSerialPort.SelectedItem.ToString() != "")
                {
                    var config = GetSettings().FindAll(c => c.SerialPort.Equals(cmbSerialPort.SelectedItem.ToString(), StringComparison.InvariantCultureIgnoreCase));
                    if (config != null && config.Count > 0)
                    {
                        int baudRate = int.Parse(config[0].BaudRate);
                        SadadStopBits stopBits = config[0].StopBits.Equals("1", StringComparison.InvariantCultureIgnoreCase) ? SadadStopBits.One : SadadStopBits.Two;

                        toolTip1.SetToolTip(cmbSerialPort, $"Baud rate: {baudRate}, Stop bits: {(int)stopBits}");
                    }
                    else
                    {
                        toolTip1.SetToolTip(cmbSerialPort, "No information exist in configuration file.");
                    }
                }
                else
                {
                    toolTip1.SetToolTip(cmbSerialPort, "No information exist in configuration file.");
                }
            }
            catch
            {

            }
        }

        private void btnMagicInquiry_Click(object sender, EventArgs e)
        {
            var mData = new MagicData();
            mData.RequestId = txtOrderId.Text;
            mData.Amount = txtPayAmount.Text;
            mData.TerminalId = txtPayTerminalId.Text;
            mData.MerchantId = txtPayMerchantId.Text;
            mData.BillNo = txtPayId.Text;

            PcPos.MagicData = mData;
            PcPos.DeviceType = DeviceType.Magic;
            PcPos.ComPortName = cmbSerialPort.SelectedItem.ToString();

            PcPos.MagicInquiry(RRN.Text);
        }

        #region Save to DB

        private static string GetFieldType(string type)
        {
            switch (type)
            {
                case "Int32":
                    return "INTEGER";
                case "Boolean":
                    return "BOOLEAN";
            }
            return "TEXT";
        }

        private void CreateSqliteDb(string name, bool forceNew = false)
        {
            if (!File.Exists(name))
            {
                SQLiteConnection.CreateFile(name);
            }
        }

        private int CreateTable<T>(SQLiteConnection sqLiteConnection, string tableName, bool dropIfExists = false)
        {
            try
            {
                var tableType = typeof(T);
                PropertyInfo[] props = tableType.GetProperties();

                StringBuilder fields = new StringBuilder();

                var delim = ",";

                fields.Append("ID INTEGER PRIMARY KEY AUTOINCREMENT");

                for (int i = 0; i < props.Length; i++)
                {
                    var type = GetFieldType(props[i].PropertyType.Name);
                    //var attrs = props[i].GetCustomAttributes();

                    fields.Append(delim);
                    fields.Append(props[i].Name);
                    fields.Append(" ");
                    fields.Append(type);

                    delim = ",";
                }
                var sql = string.Empty;
                var cmd = sqLiteConnection.CreateCommand();

                if (dropIfExists)
                {
                    try
                    {
                        sql = $"DROP TABLE IF EXISTS {tableName};";
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                        sql = $"UPDATE sqlite_sequence SET seq = '0' WHERE name = {tableName};";
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                    catch { }
                }

                sql = $"CREATE TABLE IF NOT EXISTS {tableName} ({fields});";
                cmd.CommandText = sql;
                var affectedRows = cmd.ExecuteNonQuery();
                return affectedRows;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private void SaveDb(PosResult transactionResult)
        {
            try
            {
                var connectionString = new SQLiteConnectionStringBuilder() { DataSource = dbName };
                using (var con = new SQLiteConnection(connectionString.ToString()))
                {
                    con.Open();

                    //using (var dbTransaction = con.BeginTransaction())
                    {
                        var cmd = con.CreateCommand();

                        CreateTable<PosResult>(con, "Tnx");

                        var query = "INSERT INTO Tnx ({0}) VALUES ({1});";

                        var columns = new StringBuilder();
                        var values = new StringBuilder();

                        foreach (var item in transactionResult.GetType().GetProperties())
                        {
                            columns.Append(item.Name);
                            columns.AppendLine(",");

                            var val = item.GetValue(transactionResult, null) ?? Activator.CreateInstance(item.PropertyType);

                            if (item.PropertyType == typeof(string))
                                val = $"\"{val}\"";

                            values.Append(val);
                            values.AppendLine(",");
                        }

                        cmd.CommandText = string.Format(query, columns.ToString().Trim().TrimEnd(','), values.ToString().Trim().TrimEnd(','));

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #endregion


        PcPosBusiness testPcpos;

        void TestSinglePcPos()
        {
            //if (testPcpos == null)
            {
                testPcpos = new PcPosBusiness();
                testPcpos.OnSaleResult += PcPosSaleResult;
                testPcpos.OnBillPaymentResult += PcPosPayBillResult;
                testPcpos.OnInquiriedIdentifiedSaleResult += PcPos_OnInquiredSaleResult;
                testPcpos.OnGovernmentInquiriedIdentifiedSaleResult += PcPos_OnOrganizationInquiriedIdentifiedSaleResult;
                testPcpos.OnCardInfoResult += PcPos_OnCardInfoResult;
                testPcpos.OnPosDeviceInfoResult += PcPos_OnPosDeviceInfoResult;
                testPcpos.OnAccountInfoResult += PcPos_OnAccountInfoResult;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    try
                    {
                        if (button1.Text == Resources.Buy)
                        {
                            btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = false;
                            CleanResult();
                            TestSinglePcPos();

                            //set transaction id
                            if (!string.IsNullOrEmpty(txtPayId.Text))
                                testPcpos.SetSaleId(txtPayId.Text);
                            if (checkBoxCreateRandomOrderId.Checked)
                            {
                                txtOrderId.Text = GetUnixTime();
                            }
                            if (!string.IsNullOrEmpty(txtOrderId.Text))
                                testPcpos.SetOrderId(txtOrderId.Text);
                            testPcpos.SerialNo = txtPayPosSerialNo.Text;

                            if (!string.IsNullOrEmpty(txtCardInfo.Text))
                            {
                                testPcpos.SetCardInfo(txtCardInfo.Text);
                            }

                            testPcpos.Amount = txtPayAmount.Text = "12000";

                            treeViewEx1.Update();

                            var selectedIpDevice = CheckedDevices(treeViewEx1);
                            if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                            {
                                if (selectedIpDevice.Count == 0)
                                {
                                    MessageBox.Show(Resources.Error_No_Device_Choosed);
                                    return;
                                }
                                if (selectedIpDevice.Count > 1)
                                {
                                    MessageBox.Show(Resources.Error_Device_Choose_Count);
                                    return;
                                }
                                testPcpos.Ip = selectedIpDevice[0].IpAddress;
                                testPcpos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                                testPcpos.ConnectionType = PcPosConnectionType.Lan;

                                //set result call back
                                if (radioButtonAsync.Checked)
                                {
                                    //PcPos.OnSaleResult += PcPosSaleResult;
                                    testPcpos.AsyncSaleTransaction();
                                    button1.Text = Resources.Cancel_Buy;
                                }
                                else if (radioButtonSync.Checked)
                                {
                                    button1.Text = Resources.Cancel_Buy;
                                    PcPosSaleResult(null, testPcpos.SyncSaleTransaction());
                                }
                            }
                            else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                            {
                                if (cmbSerialPort.SelectedItem != null)
                                {
                                    testPcpos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                                    testPcpos.ConnectionType = PcPosConnectionType.Serial;

                                    //set result call back
                                    if (radioButtonAsync.Checked)
                                    {
                                        //PcPos.OnSaleResult += PcPosSaleResult;
                                        testPcpos.AsyncSaleTransaction();

                                        button1.Text = Resources.Cancel_Buy;
                                    }
                                    else if (radioButtonSync.Checked)
                                    {
                                        button1.Text = Resources.Cancel_Buy;
                                        var res = testPcpos.SyncSaleTransaction();
                                        PcPosSaleResult(null, res);
                                    }
                                }
                                else
                                    MessageBox.Show(Resources.Error_Choose_Serial_Port);
                            }
                            else
                                MessageBox.Show(Resources.Error_Choose_Serial_Port);

                        }
                        else if (button1.Text == Resources.Cancel_Buy)
                        {
                            testPcpos.AbortPcPosOperation();
                            btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = button1.Enabled = true;
                            button1.Text = Resources.Buy;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = button1.Enabled = true;
                            button1.Text = Resources.Buy;
                            MessageBox.Show(ex.Message);
                        }));
                    }
                }));
            }));
            t.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    try
                    {
                        if (button2.Text == Resources.Buy)
                        {
                            btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = false;
                            CleanResult();
                            TestSinglePcPos();

                            //set transaction id
                            if (!string.IsNullOrEmpty(txtPayId.Text))
                                testPcpos.SetSaleId(txtPayId.Text);
                            if (checkBoxCreateRandomOrderId.Checked)
                            {
                                txtOrderId.Text = GetUnixTime();
                            }
                            if (!string.IsNullOrEmpty(txtOrderId.Text))
                                testPcpos.SetOrderId(txtOrderId.Text);
                            testPcpos.SerialNo = txtPayPosSerialNo.Text;

                            if (!string.IsNullOrEmpty(txtCardInfo.Text))
                            {
                                testPcpos.SetCardInfo(txtCardInfo.Text);
                            }

                            testPcpos.Amount = txtPayAmount.Text = "13000";

                            treeViewEx1.Update();

                            var selectedIpDevice = CheckedDevices(treeViewEx1);
                            if (cmbConnectionType.SelectedItem.ToString().Contains("Lan"))
                            {
                                if (selectedIpDevice.Count == 0)
                                {
                                    MessageBox.Show(Resources.Error_No_Device_Choosed);
                                    return;
                                }
                                if (selectedIpDevice.Count > 1)
                                {
                                    MessageBox.Show(Resources.Error_Device_Choose_Count);
                                    return;
                                }
                                testPcpos.Ip = selectedIpDevice[0].IpAddress;
                                testPcpos.Port = Convert.ToInt32(selectedIpDevice[0].Port);
                                testPcpos.ConnectionType = PcPosConnectionType.Lan;

                                //set result call back
                                if (radioButtonAsync.Checked)
                                {
                                    //PcPos.OnSaleResult += PcPosSaleResult;
                                    testPcpos.AsyncSaleTransaction();
                                    this.BeginInvoke(new MethodInvoker(() =>
                                    {
                                        button2.Text = Resources.Cancel_Buy;
                                    }));
                                }
                                else if (radioButtonSync.Checked)
                                {
                                    this.BeginInvoke(new MethodInvoker(() =>
                                    {
                                        button2.Text = Resources.Cancel_Buy;
                                    }));
                                    PcPosSaleResult(null, testPcpos.SyncSaleTransaction());
                                }
                            }
                            else if (cmbConnectionType.SelectedItem.ToString().Contains("Serial"))
                            {
                                if (cmbSerialPort.SelectedItem != null)
                                {
                                    testPcpos.ComPortName = cmbSerialPort.SelectedItem.ToString();
                                    testPcpos.ConnectionType = PcPosConnectionType.Serial;

                                    //set result call back
                                    if (radioButtonAsync.Checked)
                                    {
                                        //PcPos.OnSaleResult += PcPosSaleResult;
                                        testPcpos.AsyncSaleTransaction();

                                        this.BeginInvoke(new MethodInvoker(() =>
                                        {
                                            button2.Text = Resources.Cancel_Buy;
                                        }));
                                    }
                                    else if (radioButtonSync.Checked)
                                    {
                                        this.BeginInvoke(new MethodInvoker(() =>
                                        {
                                            button2.Text = Resources.Cancel_Buy;
                                        }));
                                        var res = testPcpos.SyncSaleTransaction();
                                        PcPosSaleResult(null, res);
                                    }
                                }
                                else
                                    MessageBox.Show(Resources.Error_Choose_Serial_Port);
                            }
                            else
                                MessageBox.Show(Resources.Error_Choose_Serial_Port);

                        }
                        else if (button2.Text == Resources.Cancel_Buy)
                        {
                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                testPcpos.AbortPcPosOperation();
                                btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = button2.Enabled = true;
                                button2.Text = Resources.Buy;
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            btnCheckDeviceInfo.Enabled = btnCommodityBasket.Enabled = btnFoodSafety.Enabled = button2.Enabled = true;
                            button2.Text = Resources.Buy;
                            MessageBox.Show(ex.Message);
                        }));
                    }
                }));
            }));
            t.Start();
        }
    }
}