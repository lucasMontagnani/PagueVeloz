using Microsoft.EntityFrameworkCore;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using PagueVeloz.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PagueVelozDbContext _context;

        public OutboxRepository(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OutboxEvent evt)
        {
            await _context.OutboxEvents.AddAsync(evt);
        }

        public async Task UpdateAsync(OutboxEvent outboxEvent)
        {
            _context.OutboxEvents.Update(outboxEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 50)
        {
            return await _context.OutboxEvents
                .Where(e => e.ProcessedAt == null)
                .OrderBy(e => e.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }
    }
}
