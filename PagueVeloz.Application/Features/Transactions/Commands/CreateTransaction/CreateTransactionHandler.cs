using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        Metadata? Metadata = null,
        Guid? DestinationAccountId = null
    ) : IRequest<CreateTransactionResponse>;

    public record CreateTransactionResponse
    (
        Guid TransactionId,
        string Status,
        Guid AccountId,        
        double Balance,
        double ReservedBalance,
        double AvailableBalance,
        DateTime Timestamp,
        string? ErrorMessage
        //string Operation,
        //double Amount,
        //string Currency,
        //Guid? DestinationAccountId
    );

    public record Metadata(string? Description);

    public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, CreateTransactionResponse>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateTransactionHandler> _logger;

        public CreateTransactionHandler(IAccountRepository accountRepository, ITransactionRepository transactionRepository, IOutboxRepository outboxRepository, IUnitOfWork unitOfWork, ILogger<CreateTransactionHandler> logger)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _outboxRepository = outboxRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public static readonly Histogram TransactionDurationSeconds = Metrics.CreateHistogram
        (
            "transaction_duration_seconds",
            "Duração das transações em segundos",
            new[] { "operation" }
        );

        public async Task<CreateTransactionResponse> Handle(CreateTransactionCommand command, CancellationToken cancellationToken)
        {
            // 0 - Métrica de duração (Histograma)
            using var timer = TransactionDurationSeconds.WithLabels(command.Operation.ToLower()).NewTimer();

            Activity? activity = Activity.Current;
            var source = new ActivitySource("PagueVeloz.Application");
            using var handlerActivity = source.StartActivity("ProcessTransaction", ActivityKind.Internal);
            handlerActivity?.SetTag("operation", command.Operation);
            handlerActivity?.SetTag("reference_id", command.ReferenceId);

            // 1 - Idempotência — verifica se já existe uma transação com o mesmo ReferenceId
            Transaction? existing = await _transactionRepository.GetByReferenceIdAsync(command.ReferenceId);
            if (existing != null)
            {
                _logger.LogInformation("Transação idempotente detectada: {ReferenceId}", command.ReferenceId);
                Account? account = await _accountRepository.GetByIdAsync(existing.AccountId)
                                 ?? throw new KeyNotFoundException($"Conta {existing.AccountId} não encontrada.");
                return ConvertTransactionToResponse(existing, account);
            }

            // 2 - Cria o registro base
            Transaction transaction = new(
                command.ReferenceId,
                command.AccountId,
                Enum.Parse<TransactionOperation>(command.Operation, true),
                command.Amount,
                command.Currency,
                command.Metadata?.Description
            );

            await _transactionRepository.AddAsync(transaction);

            await _unitOfWork.BeginTransactionAsync();

            // 3 - Obtém conta(s) com bloqueio pessimista
            Account? sourceAccount = await _accountRepository.GetForUpdateAsync(command.AccountId)
                                   ?? throw new InvalidOperationException($"Conta {command.AccountId} não encontrada.");

            try
            {
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

                OutboxEvent outboxEvent = new(
                    type: "TransactionCreated",
                    payload: JsonSerializer.Serialize(evtPayload)
                );

                await _outboxRepository.AddAsync(outboxEvent);

                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "Transação {Operation} processada com sucesso. RefId={Ref}",
                    command.Operation,
                    command.ReferenceId
                );

                return ConvertTransactionToResponse(transaction, sourceAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar transação RefId={Ref}", command.ReferenceId);

                await _unitOfWork.RollbackAsync();
                transaction.MarkFailed(ex.Message);
                await _transactionRepository.UpdateAsync(transaction);
                return ConvertTransactionToResponse(transaction, sourceAccount);
            }
        }

        public static CreateTransactionResponse ConvertTransactionToResponse(Transaction transaction, Account account)
        {
            return new CreateTransactionResponse(
                transaction.TransactionId,
                transaction.Status.ToString(),
                transaction.AccountId,
                account.AvailableBalance,
                account.ReservedBalance,
                account.AvailableBalance + account.CreditLimit,
                transaction.Timestamp,
                transaction.ErrorMessage
                //transaction.Operation.ToString(),
                //transaction.Amount,
                //transaction.Currency,
                //transaction.DestinationAccountId
            );
        }
    }
}
