using Microsoft.AspNetCore.Mvc;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requer autenticação para todos os endpoints
    public class AcessoController : ControllerBase
    {
        private readonly IAcessoService _acessoService;
        private readonly ILogger<AcessoController> _logger;

        public AcessoController(IAcessoService acessoService, ILogger<AcessoController> logger)
        {
            _acessoService = acessoService;
            _logger = logger;
        }

        /// <summary>
        /// Verifica se o cartão tem permissão de acesso
        /// </summary>
        /// <param name="request">Dados do cartão para verificação</param>
        /// <returns>Resultado da verificação de acesso</returns>
        [HttpPost("verificar")]
        public async Task<ActionResult<AccessCheckResponseDto>> VerificarAcesso([FromBody] VerificarAcessoRequestDto request)
        {
            _logger.LogInformation("Solicitação de verificação de acesso - UID: {uid}, GUID: {guid}", request.Uid, request.Guid);

            try
            {
                // Por enquanto, usar o UID para verificação (pode ser expandido para usar GUID também)
                var resultado = await _acessoService.CheckAccessAsync(request.Uid);
                
                if (resultado.Allowed)
                {
                    return Ok(resultado);
                }
                else
                {
                    return StatusCode(403, resultado); // Forbidden
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar acesso para UID: {uid}", request.Uid);
                return StatusCode(500, new AccessCheckResponseDto 
                { 
                    Allowed = false, 
                    Message = "Erro interno do servidor" 
                });
            }
        }
    }

    /// <summary>
    /// DTO para requisição de verificação de acesso
    /// </summary>
    public class VerificarAcessoRequestDto
    {
        public string Uid { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
    }
}