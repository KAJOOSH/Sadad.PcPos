using System;
using System.Windows.Forms;

namespace PcPosSampleDll
{
    public partial class SerialPortConfig : Form
    {
        public SerialPortConfig()
        {
            InitializeComponent();
        }

        public SerialPortConfig(string comportName)
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void SerialPortConfig_Load(object sender, EventArgs e)
        {
            dataBits.SelectedItem = "8";
            parity.SelectedItem = "None";
            flowControl.SelectedItem = "None";
        }
    }
}
