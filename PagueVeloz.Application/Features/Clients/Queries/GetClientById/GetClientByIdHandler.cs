using MediatR;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Clients.Queries.GetClientById
{
    public record GetClientByIdQuery(Guid ClientId) : IRequest<ClientResponseDTO>;
    public class GetClientByIdHandler : IRequestHandler<GetClientByIdQuery, ClientResponseDTO>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IUnitOfWork _unitOfWork;
        //private readonly ILogger<GetClientByIdHandler> _logger;

        public GetClientByIdHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
        {
            _clientRepository = clientRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ClientResponseDTO> Handle(GetClientByIdQuery query, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.GetByIdAsync(query.ClientId)
                       ?? throw new KeyNotFoundException($"Cliente de ID {query.ClientId} não encontrado.");

            ClientResponseDTO response = new()
            {
                ClientId = client.ClientId,
                Name = client.Name,
                Email = client.Email,
                CreatedAt = client.CreatedAt,
                Accounts = client.Accounts?.Select(a => new AccountSummaryDTO
                {
                    AccountId = a.AccountId.ToString(),
                    AvailableBalance = a.AvailableBalance,
                    CreditLimit = a.CreditLimit,
                    Status = a.Status.ToString()
                }).ToList()
            };

            return response;
        }
    }
}
