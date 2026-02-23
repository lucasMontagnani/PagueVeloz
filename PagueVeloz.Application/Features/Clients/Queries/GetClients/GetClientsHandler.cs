using MediatR;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Clients.Queries.GetClients
{
    public record GetClientsQuery : IRequest<IEnumerable<ClientResponseDTO>>;
    public class GetClientsHandler : IRequestHandler<GetClientsQuery, IEnumerable<ClientResponseDTO>>
    {
        private readonly IQueryRepository<Client> _clientQueryRepository;

        public GetClientsHandler(IQueryRepository<Client> clientQueryRepository)
        {
            _clientQueryRepository = clientQueryRepository;
        }

        public async Task<IEnumerable<ClientResponseDTO>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Client> clients = await _clientQueryRepository.GetAllAsync();

            return clients.Select(c => new ClientResponseDTO
            {
                ClientId = c.ClientId,
                Name = c.Name,
                Email = c.Email,
                CreatedAt = c.CreatedAt
            });
        }
    }
}
