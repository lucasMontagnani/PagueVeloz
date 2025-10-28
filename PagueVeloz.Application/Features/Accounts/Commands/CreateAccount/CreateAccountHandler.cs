using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Accounts.Commands.CreateAccount
{
    public record CreateAccountCommand : IRequest<AccountResponseDTO>
    {
        public Guid ClientId { get; set; }
        public long InitialBalance { get; set; }
        public long CreditLimit { get; set; }
    }

    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, AccountResponseDTO>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateAccountHandler> _logger;

        public CreateAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork, ILogger<CreateAccountHandler> logger)
        {
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<AccountResponseDTO> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
        {
            // TODO: Substituir por validações mais robustas, possivelmente usando FluentValidation
            if (command.InitialBalance < 0) throw new ArgumentException("O saldo inicial não pode ser negativo.");
            if (command.CreditLimit < 0) throw new ArgumentException("O limite de crédito não pode ser negativo.");

            Account account = new(
                command.ClientId,
                command.InitialBalance,
                command.CreditLimit
            );

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _accountRepository.AddAsync(account);
                await _unitOfWork.CommitAsync();

                AccountResponseDTO response = new()
                {
                    AccountId = account.AccountId.ToString(),
                    ClientId = account.ClientId.ToString(),
                    AvailableBalance = account.AvailableBalance,
                    ReservedBalance = account.ReservedBalance,
                    CreditLimit = account.CreditLimit,
                    Status = account.Status.ToString()
                };

                _logger.LogInformation("Conta criada com sucesso: {AccountId}", account.AccountId);
                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Erro ao criar conta.");
                throw new Exception("Erro ao criar conta.", ex);
            }
        }
    }
}
