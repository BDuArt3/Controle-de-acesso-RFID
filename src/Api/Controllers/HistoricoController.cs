using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleDeAcesso.Api.Controllers
{
    /// <summary>
    /// Consulta somente leitura dos históricos de auditoria. Restrito a administradores.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(Policy = "AdminOnly")]
    public class HistoricoController : ControllerBase
    {
        private readonly IHistoricoService _historicoService;
        private readonly ILogger<HistoricoController> _logger;

        public HistoricoController(IHistoricoService historicoService, ILogger<HistoricoController> logger)
        {
            _historicoService = historicoService;
            _logger = logger;
        }

        [HttpGet("usuario/{id}/historico")]
        public Task<ActionResult<PagedResultDto<HistoricoStatusAtivoDto>>> GetUsuario(
            string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => ExecuteAsync(() => _historicoService.GetUsuarioAsync(id, page, pageSize), "Usuário", id);

        [HttpGet("funcionario/{id}/historico")]
        public Task<ActionResult<PagedResultDto<HistoricoStatusAtivoDto>>> GetFuncionario(
            string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => ExecuteAsync(() => _historicoService.GetFuncionarioAsync(id, page, pageSize), "Funcionário", id);

        [HttpGet("cartao/{id}/historico")]
        public Task<ActionResult<HistoricoCartaoDto>> GetCartao(
            string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => ExecuteAsync(() => _historicoService.GetCartaoAsync(id, page, pageSize), "Cartão", id);

        private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T?>> action, string entidade, string id)
            where T : class
        {
            try
            {
                var result = await action();
                if (result == null)
                {
                    return NotFound(new { message = $"{entidade} não encontrado" });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar histórico. Entidade: {Entidade}, Id: {Id}", entidade, id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
