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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly PagueVelozDbContext _context;

        public TransactionRepository(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByReferenceIdAsync(Guid referenceId)
        {
            return await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId);
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
