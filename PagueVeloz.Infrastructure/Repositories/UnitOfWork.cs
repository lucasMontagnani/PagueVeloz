using Microsoft.EntityFrameworkCore.Storage;
using PagueVeloz.Domain.Interfaces.Repositories;
using PagueVeloz.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly PagueVelozDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(PagueVelozDbContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
                return; // já existe uma transação ativa

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("Nenhuma transação ativa para commit.");

            await _context.SaveChangesAsync();
            await _currentTransaction.CommitAsync();

            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        public async Task RollbackAsync()
        {
            if (_currentTransaction == null)
                return;

            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
                await _currentTransaction.DisposeAsync();
        }
    }
}
