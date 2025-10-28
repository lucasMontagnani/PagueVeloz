using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Features.Accounts.Commands.CreateAccount;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Tests.Application
{
    public class CreateAccountHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CreateAccountHandler>> _loggerMock;
        private readonly CreateAccountHandler _handler;

        public CreateAccountHandlerTests()
        {
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CreateAccountHandler>>();

            _handler = new CreateAccountHandler(
                _accountRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object
            );
        }

        [Fact(DisplayName = "Deve criar conta com sucesso quando dados são válidos")]
        public async Task Handle_ShouldCreateAccount_WhenDataIsValid()
        {
            // Arrange
            CreateAccountCommand command = new()
            {
                ClientId = Guid.NewGuid(),
                InitialBalance = 1000,
                CreditLimit = 500
            };

            // Act
            AccountResponseDTO result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AvailableBalance.Should().Be(1000);
            result.CreditLimit.Should().Be(500);
            result.Status.Should().Be("Active");

            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact(DisplayName = "Deve lançar exceção quando saldo inicial for negativo")]
        public async Task Handle_ShouldThrowException_WhenInitialBalanceIsNegative()
        {
            // Arrange
            CreateAccountCommand command = new()
            {
                ClientId = Guid.NewGuid(),
                InitialBalance = -100,
                CreditLimit = 500
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("O saldo inicial não pode ser negativo.");

            _accountRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact(DisplayName = "Deve realizar rollback quando ocorrer erro inesperado")]
        public async Task Handle_ShouldRollback_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            CreateAccountCommand command = new()
            {
                ClientId = Guid.NewGuid(),
                InitialBalance = 500,
                CreditLimit = 100
            };

            _accountRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Account>()))
                .ThrowsAsync(new Exception("Erro no repositório"));

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Erro ao criar conta.*");

            _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        }
    }
}
