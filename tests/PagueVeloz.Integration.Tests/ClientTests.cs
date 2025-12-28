using PagueVeloz.Application.Features.Clients.Commands.CreateClient;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Application.Features.Clients.Queries.GetClientById;
using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Integration.Tests
{
    public class ClientTests : BaseIntegrationTest
    {
        public ClientTests(IntegrationTestWebAppFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateClient_ShouldAdd_WhenCommandIsValid()
        {
            // Arrange
            CreateClientCommand command = new("Lucas Client", "lucas.client@test.com");

            // Act
            ClientResponseDTO createdClient = await Sender.Send(command);

            // Assert
            Client? clientFromDatabase = DbContext.Clients.FirstOrDefault(c => c.ClientId == createdClient.ClientId);

            Assert.NotNull(clientFromDatabase);
        }

        [Fact]
        public async Task GetClientById_ShouldReturnClient_WhenClientExists()
        {
            // Arrange
            CreateClientCommand command = new("Lucas Client 2", "lucas.client2@test.com");
            ClientResponseDTO createdClient = await Sender.Send(command);
            GetClientByIdQuery query = new(createdClient.ClientId);

            // Act
            ClientResponseDTO clientFromDatabase = await Sender.Send(query);

            // Assert

            Assert.NotNull(clientFromDatabase);
        }
    }

}
