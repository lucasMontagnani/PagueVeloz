using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid accountId);
        Task<Account?> GetForUpdateAsync(Guid accountId);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
    }
}
