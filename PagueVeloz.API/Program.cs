using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PagueVeloz.API.Extensions;
using PagueVeloz.Application.Common.Behaviours;
using PagueVeloz.Domain.Interfaces.Repositories;
using PagueVeloz.Domain.Interfaces.Repositories.Generics;
using PagueVeloz.Domain.Interfaces.Services;
using PagueVeloz.Infrastructure.Persistence.Context;
using PagueVeloz.Infrastructure.Repositories;
using PagueVeloz.Infrastructure.Repositories.Generics;
using PagueVeloz.Infrastructure.Resiliences;
using PagueVeloz.Infrastructure.Services;
using Prometheus;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth();

builder.Services.AddDbContext<PagueVelozDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurando logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();
// Assim, cada log que for gerado dentro do Activity carrega o mesmo contexto distribuído, e ferramentas como Grafana Loki ou Seq podem correlacionar logs, traces e métricas.
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.ParentId;
});

// Registrando dependências
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
builder.Services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();

//builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));

var myHandlers = AppDomain.CurrentDomain.Load("PagueVeloz.Application");
builder.Services.AddMediatR(cfg =>
{
    //cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.RegisterServicesFromAssemblies(myHandlers);
});

// Configurando Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        )
        .WriteTo.File("logs/pagueveloz-.log", rollingInterval: RollingInterval.Day);
});

// Configurando OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("PagueVeloz.Application")
            .AddConsoleExporter() // ou .AddZipkinExporter(o => o.Endpoint = new Uri("http://localhost:9411/api/v2/spans"))
            .SetResourceBuilder(
                OpenTelemetry.Resources.ResourceBuilder.CreateDefault().AddService("PagueVeloz.API")
            );
    });

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Adicionando Autenticação e Autorização
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
            ),

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    PagueVelozDbContext db = scope.ServiceProvider.GetRequiredService<PagueVelozDbContext>();
    db.Database.Migrate(); // Aplica automaticamente as migrations
}

app.MapHealthChecks("/health");

// Prometheus metrics
app.UseHttpMetrics(); // Coleta métricas HTTP automaticamente
app.MapMetrics();     // rota padrão: /metrics

app.Run();

public partial class Program { } // Para permitir testes de integração