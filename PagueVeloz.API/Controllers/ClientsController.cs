using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Application.Features.Clients.Commands.AuthenticateClient;
using PagueVeloz.Application.Features.Clients.Commands.CreateClient;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Application.Features.Clients.Queries.GetClientById;
using PagueVeloz.Application.Features.Clients.Queries.GetClients;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(IMediator mediator, ILogger<ClientsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("LoginClient")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginClient([FromBody] AuthenticateClientCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ClientResponseDTO response = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetClientById), new { id = response.ClientId }, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetClientById(Guid id)
        {
            ClientResponseDTO response = await _mediator.Send(new GetClientByIdQuery(id));
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            IEnumerable<ClientResponseDTO> response = await _mediator.Send(new GetClientsQuery());
            return Ok(response);
        }
    }
}
