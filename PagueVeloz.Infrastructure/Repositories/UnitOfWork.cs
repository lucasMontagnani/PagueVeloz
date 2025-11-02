using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PagueVeloz.Domain.Interfaces.Repositories;
using PagueVeloz.Infrastructure.Persistence.Context;
using PagueVeloz.Infrastructure.Resiliences;
using Polly.Retry;
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
        private readonly ILogger<UnitOfWork> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public UnitOfWork(PagueVelozDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
            _retryPolicy = RetryPolicyFactory.ApplyRetryPolicy(_logger);
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
                return; // já existe uma transação ativa

            _currentTransaction = await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Iniciando transação...");
                return await _context.Database.BeginTransactionAsync();
            });
        }

        public async Task CommitAsync()
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Tentando commit da transação...");
                await _context.SaveChangesAsync();
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync();
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                    _logger.LogInformation("Commit realizado com sucesso.");
                } 
                else _logger.LogWarning("Nenhuma transação ativa para realizar o commit.");
            });
        }

        public async Task RollbackAsync()
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync();
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                    _logger.LogWarning("Rollback executado com sucesso.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar rollback.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
                await _currentTransaction.DisposeAsync();

            await _context.DisposeAsync();
        }
    }
}
