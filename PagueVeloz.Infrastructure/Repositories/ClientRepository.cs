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
    public class ClientRepository : IClientRepository
    {
        private readonly PagueVelozDbContext _context;

        public ClientRepository(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByIdAsync(Guid clientId)
        {
            return await _context.Set<Client>()
                .Include(c => c.Accounts)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == clientId);
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            return await _context.Set<Client>()
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Client client)
        {
            await _context.Set<Client>().AddAsync(client);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Set<Client>().AnyAsync(c => c.Email == email);
        }
    }
}
