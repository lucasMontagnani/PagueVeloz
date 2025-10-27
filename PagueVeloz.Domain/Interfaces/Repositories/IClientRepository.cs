using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(Guid clientId);
        Task<IEnumerable<Client>> GetAllAsync();
        Task AddAsync(Client client);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
