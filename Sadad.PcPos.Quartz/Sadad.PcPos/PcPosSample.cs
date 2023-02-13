using Sadad.Newtonsoft.Json;
using Sadad.PcPos.Core;
using Sadad.PcPos.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sadad.PcPos
{
	public class PcPosSample
	{
		private static string _ip = "172.16.33.171";
		private static int[] _retryTimeOut = new[] { 5000, 5000, 5000 };
		private static int[] _responseTimeout = new[] { 120000, 5000, 5000 };

		public static void Create(Guid Id, decimal amount)
		{
			var pcPosBusiness = new PcPosBusiness();
			pcPosBusiness.ClearAmount();
			pcPosBusiness.ClearBillInfo();
			pcPosBusiness.ClearCardInfo();
			pcPosBusiness.ClearMultiSaleId();
			pcPosBusiness.ClearOrderId();
			pcPosBusiness.ClearSaleId();

			pcPosBusiness.Amount = amount.ToString();
			pcPosBusiness.RetryTimeOut = _retryTimeOut;
			pcPosBusiness.ResponseTimeOut = _responseTimeout;
			pcPosBusiness.ConnectionType = PcPosConnectionType.Lan;
			pcPosBusiness.Ip = _ip;
			pcPosBusiness.SetOrderId(Id.ToString());
			pcPosBusiness.OnSaleResult += PcPosSaleResult;

			pcPosBusiness.AsyncSaleTransaction();

		}

		private static async void PcPosSaleResult(object sender, PosResult pPosResult)
		{
			using (var _context = new dbContext())
			{
				var payment = await _context.Payments.FindAsync(Guid.Parse(pPosResult.OrderId));
				payment.PayInformation = JsonConvert.SerializeObject(pPosResult);

				_context.Payments.AddOrUpdate(payment);
				await _context.SaveChangesAsync();
			}
			await Jobs.Scheduler.ResumeJob();
		}

	}
}
