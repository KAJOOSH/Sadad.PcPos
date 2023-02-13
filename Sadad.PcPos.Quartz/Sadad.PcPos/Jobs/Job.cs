using Quartz;
using Sadad.PcPos.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Sadad.PcPos.Jobs
{
	[DisallowConcurrentExecution]
	public class Job : IJob
	{
		public async Task Execute(IJobExecutionContext context)
		{
			using (var _context = new dbContext())
			{
				var payment = await _context.Payments
					.Where(x => x.IsPaid == false && x.PayInformation == null)
					.FirstOrDefaultAsync();

				if (payment != null)
				{
					await Scheduler.PauseJob();
					PcPosSample.Create(payment.Id, payment.Amount);
				}
			}
		}
	}
}
