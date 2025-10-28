using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Transaction = PagueVeloz.Domain.Entities.Transaction;

namespace PagueVeloz.Application.Features.Transactions.Commands.CreateTransaction
{
    public record CreateTransactionCommand
    (
        string Operation,
        Guid AccountId,
        double Amount,
        string Currency,
        Guid ReferenceId,
        string? Metadata = null,
        Guid? DestinationAccountId = null
    ) : IRequest<CreateTransactionResponse>;

    public record CreateTransactionResponse
    (
        Guid TransactionId,
        string Status,
        string Operation,
        Guid AccountId,
        double Amount,
        string Currency,
        DateTime Timestamp,
        string? ErrorMessage,
        Guid? DestinationAccountId
    );

    public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionResponse>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateTransactionHandler> _logger;

        public CreateTransactionHandler(IAccountRepository accountRepository, ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, ILogger<CreateTransactionHandler> logger)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreateTransactionResponse> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
        {
            // 1 - Idempotência — verifica se já existe uma transação com o mesmo ReferenceId
            Transaction? existing = await _transactionRepository.GetByReferenceIdAsync(command.ReferenceId);
            if (existing != null)
            {
                _logger.LogInformation("Transação idempotente detectada: {ReferenceId}", command.ReferenceId);
                return ConvertTransactionToResponse(existing); ;
            }

            // 2 - Cria o registro base
            Transaction transaction = new(
                command.ReferenceId,
                command.AccountId,
                Enum.Parse<TransactionOperation>(command.Operation, true),
                command.Amount,
                command.Currency,
                command.Metadata
            );

            await _transactionRepository.AddAsync(transaction);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 3 - Obtém conta(s) com bloqueio pessimista
                Account? sourceAccount = await _accountRepository.GetForUpdateAsync(command.AccountId) 
                                       ?? throw new InvalidOperationException($"Conta {command.AccountId} não encontrada.");

                Account? destinationAccount = null;
                if (command.DestinationAccountId is not null)
                {
                    destinationAccount = await _accountRepository.GetForUpdateAsync((Guid)command.DestinationAccountId);
                    if (destinationAccount == null)
                        throw new InvalidOperationException($"Conta destino {command.DestinationAccountId} não encontrada.");
                }

                // 4 - Executa a operação conforme o tipo
                switch (transaction.Operation)
                {
                    case TransactionOperation.Credit:
                        sourceAccount.Credit(command.Amount);
                        break;

                    case TransactionOperation.Debit:
                        sourceAccount.Debit(command.Amount);
                        break;

                    case TransactionOperation.Reserve:
                        sourceAccount.Reserve(command.Amount);
                        break;

                    case TransactionOperation.Capture:
                        sourceAccount.Capture(command.Amount);
                        break;

                    case TransactionOperation.Reversal:
                        sourceAccount.Credit(command.Amount);
                        break;

                    case TransactionOperation.Transfer:
                        if (destinationAccount == null)
                            throw new InvalidOperationException("Conta destino é obrigatória para transferências.");

                        // evita deadlocks: locks já foram obtidos por ordem de AccountId
                        sourceAccount.Debit(command.Amount);
                        destinationAccount.Credit(command.Amount);
                        break;

                    default:
                        throw new InvalidOperationException($"Operação inválida: {command.Operation}");
                }

                // Marca a transação como sucesso
                transaction.MarkSuccess();

                // Cria evento de outbox
                var evtPayload = new
                {
                    transaction.TransactionId,
                    transaction.ReferenceId,
                    transaction.Operation,
                    transaction.Amount,
                    transaction.Status,
                    transaction.Timestamp,
                    SourceAccount = sourceAccount.AccountId,
                    DestinationAccount = destinationAccount?.AccountId
                };

                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "Transação {Operation} processada com sucesso. RefId={Ref}",
                    command.Operation,
                    command.ReferenceId
                );

                return ConvertTransactionToResponse(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar transação RefId={Ref}", command.ReferenceId);

                await _unitOfWork.RollbackAsync();
                transaction.MarkFailed(ex.Message);
                await _transactionRepository.UpdateAsync(transaction);
                return ConvertTransactionToResponse(transaction);
            }
        }

        public static CreateTransactionResponse ConvertTransactionToResponse(Transaction transaction)
        {
            return new CreateTransactionResponse(
                transaction.TransactionId,
                transaction.Status.ToString(),
                transaction.Operation.ToString(),
                transaction.AccountId,
                transaction.Amount,
                transaction.Currency,
                transaction.Timestamp,
                transaction.ErrorMessage,
                transaction.DestinationAccountId
            );
        }
    }
}
