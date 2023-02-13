using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sadad.PcPos.Models
{
	public class Payments
	{
		[Key]
		public Guid Id { get; set; }
		public int PosId { get; set; }
		public decimal Amount { get; set; }
		public bool IsPaid { get; set; }
		public string PayInformation { get; set; }

	}
}
