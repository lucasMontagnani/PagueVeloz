using PagueVeloz.Application.Features.Accounts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Clients.DTOs
{
    public class ClientResponseDTO
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public List<AccountSummaryDTO>? Accounts { get; set; }
    }
}
