using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Features.Accounts.Commands.CreateAccount;
using PagueVeloz.Application.Features.Accounts.DTOs;
using PagueVeloz.Application.Features.Accounts.Queries.GetAccountById;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IMediator mediator, ILogger<AccountsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                AccountResponseDTO response = await _mediator.Send(request);

                return CreatedAtAction(nameof(GetAccountById), new { id = response.AccountId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar conta.");
                return StatusCode(500, new { message = "Erro interno no servidor." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById([FromRoute] Guid id)
        {
            AccountResponseDTO response = await _mediator.Send(new GetAccountByIdQuery(id));

            return Ok(response);
        }
    }
}
