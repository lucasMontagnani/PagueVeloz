using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Application.Features.Clients.Commands.CreateClient;
using PagueVeloz.Application.Features.Clients.DTOs;
using PagueVeloz.Application.Features.Clients.Queries.GetClientById;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(IMediator mediator, ILogger<ClientsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
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
    }
}
