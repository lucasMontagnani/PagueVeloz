using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Features.Clients.Commands.CreateClient;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Tests.Application
{
    public class CreateClientHandlerTests
    {
        private readonly Mock<IClientRepository> _clientRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly CreateClientHandler _handler;
        private readonly Mock<ILogger<CreateClientHandler>> _loggerMock;

        public CreateClientHandlerTests()
        {
            _clientRepositoryMock = new Mock<IClientRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateClientHandler>>();

            _handler = new CreateClientHandler(
                _clientRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        [Fact(DisplayName = "Deve criar cliente com sucesso quando email não existir")]
        public async Task Handle_ShouldCreateClient_WhenEmailIsUnique()
        {
            // Arrange
            CreateClientCommand command = new("Nome Usuário Teste", "usuario.teste@gmail.com");

            _clientRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(false);

            // Act
            ClientResponseDTO result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Nome Usuário Teste");
            result.Email.Should().Be("usuario.teste@gmail.com");

            _clientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Client>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact(DisplayName = "Deve lançar exceção quando email já existir")]
        public async Task Handle_ShouldThrowException_WhenEmailAlreadyExists()
        {
            // Arrange
            CreateClientCommand command = new("Nome Usuário Teste", "usuario.teste@gmail.com");

            _clientRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Já existe um cliente com o email fornecido.");

            _clientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Client>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact(DisplayName = "Deve fazer rollback quando ocorrer erro inesperado")]
        public async Task Handle_ShouldRollback_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            CreateClientCommand command = new("Nome Usuário Teste", "usuario.teste@gmail.com");

            _clientRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(command.Email))
                .ReturnsAsync(false);

            _clientRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Client>()))
                .ThrowsAsync(new Exception("Erro de banco"));

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Erro ao cadastrar cliente.*");

            _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        }
    }
}
