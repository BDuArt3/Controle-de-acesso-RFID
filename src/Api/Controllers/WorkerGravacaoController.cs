using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleDeAcesso.Api.Controllers;

[ApiController]
[Route("api/cartao")]
[Authorize(Policy = "WorkerCallback")]
public sealed class WorkerGravacaoController : ControllerBase
{
    private readonly ICartaoService _cartaoService;
    private readonly ILogger<WorkerGravacaoController> _logger;

    public WorkerGravacaoController(
        ICartaoService cartaoService,
        ILogger<WorkerGravacaoController> logger)
    {
        _cartaoService = cartaoService;
        _logger = logger;
    }

    [HttpPost("confirmar-gravacao")]
    public async Task<ActionResult<GravacaoCartaoResponseDto>> ConfirmarGravacao(
        [FromBody] ConfirmarGravacaoCartaoDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _cartaoService.ConfirmarGravacaoAsync(request, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = "Solicitação de gravação não encontrada." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao confirmar gravação de cartão.");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    [HttpPost("falhar-gravacao")]
    public async Task<ActionResult<GravacaoCartaoResponseDto>> FalharGravacao(
        [FromBody] FalharGravacaoCartaoDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _cartaoService.RegistrarFalhaGravacaoAsync(request, cancellationToken);
            return result == null
                ? NotFound(new { message = "Solicitação de gravação não encontrada." })
                : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar falha de gravação.");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}
