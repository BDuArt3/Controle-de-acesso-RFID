using Microsoft.AspNetCore.Mvc;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ControleDeAcesso.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requer autenticação para todos os endpoints
    public class CartaoController : ControllerBase
    {
        private readonly ICartaoService _cartaoService;
        private readonly ILogger<CartaoController> _logger;
        private readonly IHubContext<GravacaoHub> _hubContext;

        public CartaoController(
            ICartaoService cartaoService, 
            ILogger<CartaoController> logger,
            IHubContext<GravacaoHub> hubContext)
        {
            _cartaoService = cartaoService;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Cria e grava um novo cartão RFID
        /// O GUID é gerado automaticamente pelo sistema para garantir segurança
        /// </summary>
        /// <param name="gravarCartaoDto">Dados da solicitação de gravação</param>
        /// <returns>Status da solicitação de gravação</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOrOperador")] // Admins e Operadores podem gravar cartões
        public async Task<ActionResult<GravacaoCartaoResponseDto>> SolicitarGravacao(
            [FromBody] GravarCartaoDto gravarCartaoDto,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var funcionarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(funcionarioId))
                {
                    return Unauthorized(new { message = "Funcionário autenticado sem identificador válido." });
                }

                var result = await _cartaoService.SolicitarGravacaoAsync(
                    gravarCartaoDto, funcionarioId, idempotencyKey, cancellationToken);

                if (result.Command != null &&
                    result.Response.Status == "Pendente" &&
                    GravacaoHub.HasConnectedWorker)
                {
                    await DespacharComandoAsync(result.Command, cancellationToken);
                    var updated = await _cartaoService.GetSolicitacaoAsync(
                        result.Command.SolicitacaoId, cancellationToken);
                    return AcceptedAtAction(nameof(GetSolicitacao), new { id = result.Command.SolicitacaoId }, updated);
                }

                return AcceptedAtAction(
                    nameof(GetSolicitacao),
                    new { id = result.Response.SolicitacaoId },
                    result.Response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao solicitar gravação de cartão");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        [HttpGet("solicitacoes/{id}")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult<GravacaoCartaoResponseDto>> GetSolicitacao(
            string id,
            CancellationToken cancellationToken)
        {
            var result = await _cartaoService.GetSolicitacaoAsync(id, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("solicitacoes/{id}/retentar")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult<GravacaoCartaoResponseDto>> RetentarGravacao(
            string id,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!GravacaoHub.HasConnectedWorker)
                {
                    var current = await _cartaoService.GetSolicitacaoAsync(id, cancellationToken);
                    return current == null
                        ? NotFound(new { message = "Solicitação de gravação não encontrada." })
                        : StatusCode(StatusCodes.Status503ServiceUnavailable, current);
                }

                var command = await _cartaoService.PrepararRetentativaAsync(id, cancellationToken);
                if (command == null)
                {
                    return NotFound(new { message = "Solicitação de gravação não encontrada." });
                }

                await DespacharComandoAsync(command, cancellationToken);
                var updated = await _cartaoService.GetSolicitacaoAsync(id, cancellationToken);
                return AcceptedAtAction(nameof(GetSolicitacao), new { id }, updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("solicitacoes/{id}/cancelar")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult<GravacaoCartaoResponseDto>> CancelarSolicitacao(
            string id,
            [FromBody] CancelarGravacaoCartaoDto request,
            CancellationToken cancellationToken)
        {
            var funcionarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(funcionarioId))
            {
                return Unauthorized(new { message = "Funcionário autenticado sem identificador válido." });
            }

            try
            {
                var result = await _cartaoService.CancelarSolicitacaoAsync(
                    id, funcionarioId, request.Motivo, cancellationToken);
                return result == null ? NotFound(new { message = "Solicitação de gravação não encontrada." }) : Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        private async Task DespacharComandoAsync(
            GravacaoCartaoCommandDto command,
            CancellationToken cancellationToken)
        {
            if (!await _cartaoService.RegistrarDespachoAsync(command.SolicitacaoId, cancellationToken))
            {
                return;
            }

            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceberComandoGravacao", command, cancellationToken);
            }
            catch
            {
                await _cartaoService.RegistrarFalhaGravacaoAsync(
                    new FalharGravacaoCartaoDto
                    {
                        SolicitacaoId = command.SolicitacaoId,
                        Motivo = "Não foi possível despachar o comando ao Worker."
                    },
                    CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// Busca um cartão por ID
        /// </summary>
        /// <param name="id">ID do cartão</param>
        /// <returns>Cartão encontrado</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CartaoDto>> GetById(string id)
        {
            _logger.LogInformation("Requisição para buscar cartão por ID: {Id}", id);

            try
            {
                var cartao = await _cartaoService.GetByIdAsync(id);
                
                if (cartao == null)
                {
                    return NotFound(new { message = "Cartão não encontrado" });
                }

                return Ok(cartao);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Busca um cartão por UID
        /// </summary>
        /// <param name="uid">UID do cartão</param>
        /// <returns>Cartão encontrado</returns>
        [HttpGet("uid/{uid}")]
        public async Task<ActionResult<CartaoDto>> GetByUid(string uid)
        {
            _logger.LogInformation("Requisição para buscar cartão por UID: {Uid}", uid);

            try
            {
                var cartao = await _cartaoService.GetByUidAsync(uid);
                
                if (cartao == null)
                {
                    return NotFound(new { message = "Cartão não encontrado" });
                }

                return Ok(cartao);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por UID: {Uid}", uid);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Lista todos os cartões (incluindo inativos)
        /// </summary>
        /// <returns>Lista de todos os cartões</returns>
        [HttpGet]
        [Authorize(Policy = "TodosFuncionarios")] // Todos os funcionários podem listar cartões
        public async Task<ActionResult<IEnumerable<CartaoDto>>> GetAll()
        {
            _logger.LogInformation("Requisição para listar todos os cartões (incluindo inativos)");

            try
            {
                var cartoes = await _cartaoService.GetAllAsync();
                return Ok(cartoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar cartões");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Lista cartões de um usuário específico
        /// </summary>
        /// <param name="usuarioId">ID do usuário</param>
        /// <returns>Lista de cartões do usuário</returns>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<CartaoDto>>> GetByUsuarioId(string usuarioId)
        {
            _logger.LogInformation("Requisição para listar cartões do usuário: {UsuarioId}", usuarioId);

            try
            {
                var cartoes = await _cartaoService.GetByUsuarioIdAsync(usuarioId);
                return Ok(cartoes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar cartões do usuário: {UsuarioId}", usuarioId);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza um cartão existente
        /// </summary>
        /// <param name="id">ID do cartão</param>
        /// <param name="updateCartaoDto">Dados atualizados do cartão</param>
        /// <returns>Cartão atualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult<CartaoDto>> Update(string id, [FromBody] CreateCartaoDto updateCartaoDto)
        {
            _logger.LogInformation("Requisição para atualizar cartão. ID: {Id}, UID: {Uid}", 
                id, updateCartaoDto.Uid);

            try
            {
                var cartao = await _cartaoService.UpdateAsync(id, updateCartaoDto);
                return Ok(cartao);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cartão. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Desativa um cartão (exclusão lógica)
        /// </summary>
        /// <param name="id">ID do cartão</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult> Delete(string id, [FromBody] AlterarStatusCartaoDto request)
        {
            _logger.LogInformation("Requisição para desativar cartão. ID: {Id}", id);

            try
            {
                var funcionarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(funcionarioId))
                {
                    return Unauthorized(new { message = "Funcionário autenticado sem identificador válido." });
                }

                var resultado = await _cartaoService.BlockAsync(id, funcionarioId, request.Motivo);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Cartão não encontrado" });
                }

                return Ok(new { message = "Cartão bloqueado com sucesso" });
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
                _logger.LogError(ex, "Erro ao desativar cartão. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Reativa um cartão
        /// </summary>
        /// <param name="id">ID do cartão</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("{id}/ativar")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult> Activate(string id, [FromBody] AlterarStatusCartaoDto request)
        {
            _logger.LogInformation("Requisição para ativar cartão. ID: {Id}", id);

            try
            {
                var funcionarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(funcionarioId))
                {
                    return Unauthorized(new { message = "Funcionário autenticado sem identificador válido." });
                }

                var resultado = await _cartaoService.UnblockAsync(id, funcionarioId, request.Motivo);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Cartão não encontrado" });
                }

                return Ok(new { message = "Cartão ativado com sucesso" });
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
                _logger.LogError(ex, "Erro ao ativar cartão. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        [HttpPost("{id}/cancelar")]
        [Authorize(Policy = "AdminOrOperador")]
        public async Task<ActionResult> Cancel(string id, [FromBody] AlterarStatusCartaoDto request)
        {
            var funcionarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(funcionarioId))
            {
                return Unauthorized(new { message = "Funcionário autenticado sem identificador válido." });
            }

            try
            {
                var result = await _cartaoService.CancelAsync(id, funcionarioId, request.Motivo);
                return result
                    ? Ok(new { message = "Cartão cancelado permanentemente." })
                    : NotFound(new { message = "Cartão não encontrado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }

}