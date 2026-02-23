using MediatR;
using PagueVeloz.Application.Common.Helpers;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces.Repositories.Generics;
using PagueVeloz.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Application.Features.Clients.Commands.AuthenticateClient
{
    public record AuthenticateClientCommand
    (
        string Email,
        string Senha
    ) : IRequest<string>;

    public class AuthenticateClientHandler : IRequestHandler<AuthenticateClientCommand, string>
    {
        private readonly IQueryRepository<Client> _clientQueryRepository;
        private readonly ITokenService _tokenService;

        public AuthenticateClientHandler(IQueryRepository<Client> clientQueryRepository, ITokenService tokenService)
        {
            _clientQueryRepository = clientQueryRepository;
            _tokenService = tokenService;
        }

        public async Task<string> Handle(AuthenticateClientCommand command, CancellationToken cancellationToken)
        {
            Client client = await _clientQueryRepository.GetFirstOrDefaultWithFilterAsync(c => c.Email == command.Email)
                          ?? throw new Exception("Email não encontrado.");

            if (client.Senha != UtilsHelper.EncryptPassword(command.Senha))
                throw new Exception("Senha incorreta.");

            string token = _tokenService.GenerateToken(client);

            return token;
        }
    }
}
