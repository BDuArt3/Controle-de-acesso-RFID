using Microsoft.AspNetCore.Mvc;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [Authorize(Policy = "WorkerCallback")]
        public async Task<ActionResult<AccessCheckResponseDto>> VerificarAcesso([FromBody] VerificarAcessoRequestDto request)
        {
            _logger.LogInformation(
                "Solicitação de verificação de acesso - UID físico: {uid}, GUID/Credencial: {guid}, Local: {local}",
                request.Uid,
                request.Guid,
                request.Local);

            try
            {
                // A validação de acesso busca pela credencial gravada (Guid); se ausente, faz fallback para Uid
                var credencial = !string.IsNullOrWhiteSpace(request.Guid) ? request.Guid : request.Uid;
                var resultado = await _acessoService.CheckAccessAsync(credencial, request.Local);
                
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
                _logger.LogError(ex, "Erro ao verificar acesso para UID: {uid}, GUID: {guid}", request.Uid, request.Guid);
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

        [StringLength(100)]
        public string Local { get; set; } = ControleDeAcesso.Core.Entities.LogAcesso.LocalPadrao;
    }
}