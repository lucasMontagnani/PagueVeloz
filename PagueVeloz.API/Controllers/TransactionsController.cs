using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Features.Transactions.Commands.CreateTransaction;
using PagueVeloz.Domain.Entities;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Diagnostics;

namespace PagueVeloz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TransactionsController> _logger;
        private static readonly ActivitySource ActivitySource = new(nameof(TransactionsController));

        public TransactionsController(IMediator mediator, ILogger<TransactionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessTransaction([FromBody] CreateTransactionCommand request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var activity = ActivitySource.StartActivity("CreateTransactionRequest");
            activity?.SetTag("operation", request.Operation);
            activity?.SetTag("account_id", request.AccountId);
            activity?.SetTag("reference_id", request.ReferenceId);

            try
            {
                CreateTransactionResponse response = await _mediator.Send(request);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro ao processar transação de referência {Reference}", request.ReferenceId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar transação de referência {Reference}", request.ReferenceId);
                return StatusCode(500, new { message = "Erro interno no servidor." });
            }
        }
    }
}
