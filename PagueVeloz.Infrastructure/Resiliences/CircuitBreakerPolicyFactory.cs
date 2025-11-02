using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using Polly;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly.CircuitBreaker;

namespace PagueVeloz.Infrastructure.Resiliences
{
    public static class CircuitBreakerPolicyFactory
    {
        public static AsyncCircuitBreakerPolicy ApplyCircuitBreakerPolicy<TLogger>(ILogger<TLogger> logger)
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, timespan) =>
                        logger.LogWarning("Circuit breaker aberto por {Time}s devido a erro: {Error}", timespan.TotalSeconds, ex.Message),
                    onReset: () =>
                        logger.LogInformation("Circuit breaker fechado, retomando operações."),
                    onHalfOpen: () =>
                        logger.LogInformation("Circuit breaker em modo de teste (half-open).")
    );
        }
    }
}
