using MediatR;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Clients.Commands.CreateClient
{
    public record CreateClientCommand
    (
        string Name,
        string Email
    ) : IRequest<ClientResponseDTO>;

    public class CreateClientHandler : IRequestHandler<CreateClientCommand, ClientResponseDTO>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IUnitOfWork _unitOfWork;
        //private readonly ILogger<CreateClientHandler> _logger;

        public CreateClientHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
        {
            _clientRepository = clientRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ClientResponseDTO> Handle(CreateClientCommand request, CancellationToken cancellationToken)
        {
            if (await _clientRepository.ExistsByEmailAsync(request.Email))
                throw new Exception("Já existe um cliente com o email fornecido.");

            Client client = new(request.Name, request.Email);

            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _clientRepository.AddAsync(client);
                await _unitOfWork.CommitAsync();

                ClientResponseDTO response = new()
                {
                    ClientId = client.ClientId,
                    Name = client.Name,
                    Email = client.Email,
                    CreatedAt = client.CreatedAt,
                    Accounts = new List<AccountSummaryDTO>()
                };

                //_logger.LogInformation("Cliente criado com sucesso: {ClientId}", client.ClientId);
                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                //_logger.LogError(ex, "Erro ao criar cliente.");
                throw new Exception("Erro ao cadastrar cliente.", ex);
            }
        }
    }
}
