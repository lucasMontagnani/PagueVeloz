using MediatR;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Accounts.Queries.GetAccountById
{
    public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountResponseDTO>;
    public class GetAccountByIdHandler : IRequestHandler<GetAccountByIdQuery, AccountResponseDTO>
    {
        private readonly IAccountRepository _accountRepository;

        public GetAccountByIdHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<AccountResponseDTO> Handle(GetAccountByIdQuery query, CancellationToken cancellationToken)
        {
            Account? account = await _accountRepository.GetByIdAsync(query.AccountId) 
                                             ?? throw new KeyNotFoundException($"Conta com ID {query.AccountId} não encontrada.");

            AccountResponseDTO response = new()
            {
                AccountId = account.AccountId.ToString(),
                ClientId = account.ClientId.ToString(),
                AvailableBalance = account.AvailableBalance,
                ReservedBalance = account.ReservedBalance,
                CreditLimit = account.CreditLimit,
                Status = account.Status.ToString()
            };

            return response;
        }
    }
}
