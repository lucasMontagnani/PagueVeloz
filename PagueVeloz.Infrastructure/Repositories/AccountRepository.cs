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
    public class AccountRepository : IAccountRepository
    {
        private readonly PagueVelozDbContext _context;

        public AccountRepository(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByIdAsync(Guid accountId)
        {
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<Account?> GetForUpdateAsync(Guid accountId)
        {
            var sql = "SELECT * FROM \"Accounts\" WHERE \"AccountId\" = {0} FOR UPDATE";
            return await _context.Accounts
                .FromSqlRaw(sql, accountId)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }

        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
    }
}
