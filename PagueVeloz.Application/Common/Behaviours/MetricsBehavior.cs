using MediatR;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Common.Behaviours
{
    public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private static readonly Counter _requestsCounter = Metrics.CreateCounter(
            "application_requests_total",
        "Número total de requisições processadas",
            new[] { "request", "status" });

        private static readonly Histogram _durationHistogram = Metrics.CreateHistogram(
            "application_request_duration_seconds",
            "Duração das requisições (handlers)",
            new[] { "request" });

        private readonly ILogger<MetricsBehavior<TRequest, TResponse>> _logger;

        public MetricsBehavior(ILogger<MetricsBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            using var timer = _durationHistogram.WithLabels(requestName).NewTimer();

            try
            {
                var response = await next();
                _requestsCounter.WithLabels(requestName, "success").Inc();
                return response;
            }
            catch (Exception ex)
            {
                _requestsCounter.WithLabels(requestName, "error").Inc();
                _logger.LogError(ex, "Erro ao processar {RequestName}", requestName);
                throw;
            }
        }
    }
}
