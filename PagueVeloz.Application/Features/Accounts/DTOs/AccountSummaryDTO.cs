using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Accounts.DTOs
{
    public class AccountSummaryDTO
    {
        public string AccountId { get; set; } = null!;
        public double AvailableBalance { get; set; }
        public double CreditLimit { get; set; }
        public string Status { get; set; } = null!;
    }
}
