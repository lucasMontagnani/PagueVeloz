using Microsoft.Extensions.Logging;
using Polly.Retry;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace PagueVeloz.Infrastructure.Resiliences
{
    public static class RetryPolicyFactory
    {
        public static AsyncRetryPolicy ApplyRetryPolicy<TLogger>(ILogger<TLogger> logger)
        {
            return Policy
                // Captura exceções comuns em bancos relacionais
                .Handle<DbUpdateException>()
                .Or<DbException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Erro ao acessar o banco (tentativa {Retry}) — aguardando {Delay}s. Erro: {Error}",
                            retryCount,
                            timespan.TotalSeconds,
                            exception.Message
                        );
                    });
        }
    }
}
