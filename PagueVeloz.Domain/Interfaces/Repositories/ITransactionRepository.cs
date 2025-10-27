using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByReferenceIdAsync(Guid referenceId);
        Task AddAsync(Transaction transaction);
        Task UpdateAsync(Transaction transaction);
    }
}
