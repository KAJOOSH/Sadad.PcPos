using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sadad.PcPos
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private async void MainForm_Load(object sender, EventArgs e)
		{
			await Jobs.Scheduler.Create();
		}

		private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			await Jobs.Scheduler.Shutdown();
		}
	}
}
