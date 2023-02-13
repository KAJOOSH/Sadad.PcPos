using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sadad.PcPos.Models;

namespace Sadad.PcPos.Data
{
	public class dbContext : DbContext
	{
		public dbContext()
			: base("Data Source=narges\\serverdemo;Initial Catalog=PcPos;User ID=sa;Password=sasa@1234;MultipleActiveResultSets=true;Encrypt=False;")
		{
		}
		public virtual DbSet<Payments> Payments { get; set; }

	}
}
