using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sadad.PcPos.Jobs
{
	public static class Scheduler
	{
		private static IScheduler scheduler;

		private static IJobDetail job = JobBuilder.Create<Job>()
				.WithIdentity("job1", "group1")
				.DisallowConcurrentExecution()
				.Build();

		private static ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity("trigger1", "group1")
				.StartNow()
				.WithSimpleSchedule(x => x
					.WithIntervalInSeconds(2)
					.RepeatForever()
				).Build();

		public static async Task Create()
		{
			StdSchedulerFactory factory = new StdSchedulerFactory();
			//scheduler = await factory.GetScheduler();

			var properties = new NameValueCollection();

			scheduler = await SchedulerBuilder.Create(properties)
				.UseDefaultThreadPool(x => x.MaxConcurrency = 1)
				.BuildScheduler();

			await scheduler.Clear();
			await scheduler.Start();

			await scheduler.ScheduleJob(job, trigger);
		}
		public static async Task PauseJob()
		{
			await scheduler.PauseJob(job.Key);
		}

		public static async Task ResumeJob()
		{
			await scheduler.ResumeJob(job.Key);
		}

		public static async Task Shutdown()
		{
			await scheduler.Shutdown();
		}
	}
}
