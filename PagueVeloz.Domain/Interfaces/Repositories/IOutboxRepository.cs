using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxEvent evt);
        Task<IEnumerable<OutboxEvent>> GetPendingAsync(int batchSize = 50);
    }
}
