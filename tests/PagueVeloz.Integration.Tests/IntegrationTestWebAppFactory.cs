using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PagueVeloz.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace PagueVeloz.Integration.Tests
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("db_pagueveloz_dev")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Aqui voc  pode substituir servi  os, configurar bancos de dados em mem  ria, etc.
                ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PagueVelozDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PagueVelozDbContext>(options =>
                {
                    //options.UseInMemoryDatabase("InMemoryPagueVelozTestDb");
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                });
            });
        }

        public Task InitializeAsync()
        {
            return _dbContainer.StartAsync();
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _dbContainer.StopAsync();
        }
    }
}
