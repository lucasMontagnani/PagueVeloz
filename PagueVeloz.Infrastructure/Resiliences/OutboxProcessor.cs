using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Infrastructure.Persistence.Context;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Infrastructure.Resiliences
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

        public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retryPolicy = RetryPolicyFactory.ApplyRetryPolicy(_logger);
            _circuitBreakerPolicy = CircuitBreakerPolicyFactory.ApplyCircuitBreakerPolicy(_logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PagueVelozDbContext>();

                var pendingEvents = await context.OutboxEvents
                .Where(e => e.ProcessedAt == null)
                .OrderBy(e => e.CreatedAt)
                .Take(20)
                .ToListAsync(stoppingToken);

                foreach (var evt in pendingEvents)
                {
                    try
                    {
                        // Combina as políticas: Retry + Circuit Breaker
                        await Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy)
                        .ExecuteAsync(async () =>
                        {
                            // Aqui você pode publicar no Kafka, RabbitMQ, fila etc.
                            //await PublishEventAsync(evt, stoppingToken);

                            _logger.LogInformation("Publicando evento {Type} => {Payload}", evt.Type, evt.Payload);
                            evt.MarkProcessed();
                        });
                    }
                    catch (BrokenCircuitException)
                    {
                        _logger.LogWarning("Circuit breaker ativo — ignorando publicação de eventos temporariamente.");
                        break; // interrompe o loop enquanto o circuito está aberto
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao publicar evento {Id}", evt.OutboxEventId);
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

}
