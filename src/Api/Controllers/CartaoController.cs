using Microsoft.AspNetCore.Mvc;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ControleDeAcesso.Api.Hubs;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<ActionResult<GravacaoCartaoResponseDto>> SolicitarGravacao([FromBody] GravarCartaoDto gravarCartaoDto)
        {
            _logger.LogInformation("Solicitação de gravação de cartão para usuário: {UsuarioId}", gravarCartaoDto.UsuarioId);

            try
            {
                // Gerar GUID único e seguro
                string guidGerado = Guid.NewGuid().ToString();
                
                // Registrar solicitação pendente para rastreamento
                GravacaoHub.AdicionarSolicitacaoPendente(guidGerado, gravarCartaoDto.UsuarioId);
                
                // NÃO criar o cartão ainda - aguardar confirmação do Arduino
                // O cartão será criado apenas quando o Worker confirmar a gravação bem-sucedida
                
                // Enviar comando de gravação para todos os Worker Services conectados
                await _hubContext.Clients.All.SendAsync("ReceberComandoGravacao", guidGerado);
                
                _logger.LogInformation("Comando de gravação enviado para GUID: {guid}", guidGerado);
                
                // Retornar resposta sem expor o GUID completo
                var response = new GravacaoCartaoResponseDto
                {
                    CartaoRef = guidGerado.Substring(0, 8) + "...", // Apenas primeiros 8 caracteres
                    Status = "Solicitado",
                    Mensagem = "Comando de gravação enviado. Aproxime o cartão do leitor.",
                    UsuarioId = gravarCartaoDto.UsuarioId,
                    NomeUsuario = "", // Será preenchido quando o cartão for criado
                    DataSolicitacao = DateTime.UtcNow
                };
                
                return Ok(response);
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
                _logger.LogError(ex, "Erro ao solicitar gravação de cartão");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Confirma a gravação bem-sucedida de um cartão RFID
        /// Chamado pelo Worker quando o Arduino confirma a gravação
        /// </summary>
        /// <param name="request">Dados da confirmação de gravação</param>
        /// <returns>Status da confirmação</returns>
        [HttpPost("confirmar-gravacao")]
        public async Task<ActionResult> ConfirmarGravacao([FromBody] ConfirmarGravacaoRequestDto request)
        {
            _logger.LogInformation("Confirmação de gravação de cartão para GUID: {GuidGravado}", request.GuidGravado);

            try
            {
                if (string.IsNullOrWhiteSpace(request.GuidGravado))
                {
                    return BadRequest(new { message = "GUID gravado é obrigatório" });
                }

                // Buscar a solicitação pendente para encontrar o usuário
                var usuarioId = GravacaoHub.RemoverSolicitacaoPendente(request.GuidGravado);
                
                if (string.IsNullOrEmpty(usuarioId))
                {
                    _logger.LogWarning("Solicitação de gravação não encontrada para GUID: {guid}", request.GuidGravado);
                    return BadRequest(new { message = "Solicitação de gravação não encontrada" });
                }
                
                var createCartaoDto = new CreateCartaoDto
                {
                    Uid = request.GuidGravado,
                    UsuarioId = usuarioId
                };
                
                try
                {
                    var cartao = await _cartaoService.CreateAsync(createCartaoDto);
                    _logger.LogInformation("Cartão criado com sucesso após confirmação de gravação. ID: {Id}, UID: {Uid}", 
                        cartao.Id, cartao.Uid);
                        
                    return Ok(new { 
                        message = "Cartão criado com sucesso", 
                        cartaoId = cartao.Id,
                        uid = cartao.Uid 
                    });
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("já está em uso"))
                {
                    // GUID já existe - possivelmente gravação duplicada
                    _logger.LogWarning("Tentativa de criar cartão com GUID já existente: {guid}", request.GuidGravado);
                    return Ok(new { message = "Cartão já existe no sistema" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar gravação de cartão");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
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
        /// Lista todos os cartões ativos
        /// </summary>
        /// <returns>Lista de cartões ativos</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartaoDto>>> GetAll()
        {
            _logger.LogInformation("Requisição para listar todos os cartões ativos");

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
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
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
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation("Requisição para desativar cartão. ID: {Id}", id);

            try
            {
                var resultado = await _cartaoService.DeleteAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Cartão não encontrado" });
                }

                return Ok(new { message = "Cartão desativado com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
        public async Task<ActionResult> Activate(string id)
        {
            _logger.LogInformation("Requisição para ativar cartão. ID: {Id}", id);

            try
            {
                var resultado = await _cartaoService.ActivateAsync(id);
                
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar cartão. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// DTO para confirmação de gravação de cartão
    /// </summary>
    public class ConfirmarGravacaoRequestDto
    {
        public string GuidGravado { get; set; } = string.Empty;
    }
}