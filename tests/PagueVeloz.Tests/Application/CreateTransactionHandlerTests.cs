using FluentAssertions;
using Moq;
using PagueVeloz.Application.Features.Transactions.Commands.CreateTransaction;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Tests.Application
{
    public class CreateTransactionHandlerTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly CreateTransactionHandler _handler;

        private readonly Guid _accountId = Guid.NewGuid();
        private readonly Guid _destinationId = Guid.NewGuid();
        private readonly Guid _referenceId = Guid.NewGuid();

        public CreateTransactionHandlerTests()
        {
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _handler = new CreateTransactionHandler(
                _accountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _unitOfWorkMock.Object
            );
        }

        private Account CreateAccount(double balance, double reserved = 0)
        {
            Account account = new(_accountId, balance, 0);
            typeof(Account)
                .GetProperty(nameof(Account.ReservedBalance))!
                .SetValue(account, reserved);
            return account;
        }

        // Crédito (credit): Adiciona valor ao saldo da conta
        [Fact(DisplayName = "Deve realizar operação de crédito com sucesso")]
        public async Task Handle_ShouldCreditAccount_WhenOperationIsCredit()
        {
            Account account = CreateAccount(100);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("credit", _accountId, 50, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            account.AvailableBalance.Should().Be(150);

            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        // Débito (debit): Remove valor do saldo da conta (success)
        [Fact(DisplayName = "Deve realizar operação de débito com sucesso quando saldo for suficiente")]
        public async Task Handle_ShouldDebitAccount_WhenOperationIsDebit_AndHasBalance()
        {
            Account account = CreateAccount(200);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("debit", _accountId, 50, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            account.AvailableBalance.Should().Be(150);
        }

        // Débito (debit): Remove valor do saldo da conta (failed)
        [Fact(DisplayName = "Deve falhar operação de débito quando saldo for insuficiente")]
        public async Task Handle_ShouldFailDebit_WhenInsufficientBalance()
        {
            Account account = CreateAccount(30);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("debit", _accountId, 100, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Failed");
            result.ErrorMessage.Should().Contain("Saldo insuficiente para débito.");
            _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        }

        // Reserva (reserve): Move valor do saldo disponível para o saldo reservado
        [Fact(DisplayName = "Deve reservar valor do saldo disponível com sucesso")]
        public async Task Handle_ShouldReserveFunds_WhenOperationIsReserve()
        {
            Account account = CreateAccount(200);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("reserve", _accountId, 100, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            account.AvailableBalance.Should().Be(100);
            account.ReservedBalance.Should().Be(100);
        }

        // Captura (capture): Confirma uma reserva, removendo do saldo reservado
        [Fact(DisplayName = "Deve capturar valor reservado com sucesso")]
        public async Task Handle_ShouldCaptureFunds_WhenOperationIsCapture()
        {
            Account account = CreateAccount(100, reserved: 100);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("capture", _accountId, 100, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            account.ReservedBalance.Should().Be(0);
        }

        // Estorno (reversal): Reverte uma operação anterior
        [Fact(DisplayName = "Deve estornar valor creditando novamente a conta")]
        public async Task Handle_ShouldReverseTransaction_WhenOperationIsReversal()
        {
            Account account = CreateAccount(100);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(account);

            CreateTransactionCommand command = new("reversal", _accountId, 50, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            account.AvailableBalance.Should().Be(150);
        }

        // Transferência (transfer): Move valor entre duas contas (Success)
        [Fact(DisplayName = "Deve transferir valor entre duas contas com sucesso")]
        public async Task Handle_ShouldTransferFunds_WhenOperationIsTransfer()
        {
            Account source = CreateAccount(300);
            Account dest = new Account(_destinationId, 100, 0);

            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(source);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_destinationId)).ReturnsAsync(dest);

            CreateTransactionCommand command = new("transfer", _accountId, 150, "BRL", _referenceId, null, _destinationId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Success");
            source.AvailableBalance.Should().Be(150);
            dest.AvailableBalance.Should().Be(250);
        }

        // Transferência (transfer): Move valor entre duas contas (Failed)
        [Fact(DisplayName = "Deve falhar transferência quando conta destino não for informada")]
        public async Task Handle_ShouldFailTransfer_WhenDestinationMissing()
        {
            Account source = CreateAccount(300);
            _accountRepositoryMock.Setup(r => r.GetForUpdateAsync(_accountId)).ReturnsAsync(source);

            CreateTransactionCommand command = new("transfer", _accountId, 100, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Status.Should().Be("Failed");
            result.ErrorMessage.Should().Contain("Conta destino é obrigatória para transferências.");
        }

        // Garantir idempotência através do reference_id
        [Fact(DisplayName = "Deve retornar transação existente quando ReferenceId já existir")]
        public async Task Handle_ShouldReturnExistingTransaction_WhenReferenceAlreadyExists()
        {
            Transaction existing = new(_referenceId, _accountId, TransactionOperation.Credit, 100, "BRL", null);
            _transactionRepositoryMock.Setup(r => r.GetByReferenceIdAsync(_referenceId)).ReturnsAsync(existing);

            CreateTransactionCommand command = new("credit", _accountId, 100, "BRL", _referenceId);

            CreateTransactionResponse result = await _handler.Handle(command, CancellationToken.None);

            result.TransactionId.Should().Be(existing.TransactionId);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }
    }
}
