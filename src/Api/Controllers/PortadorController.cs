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
    public class PortadorController : ControllerBase
    {
        private readonly IPortadorService _portadorService;
        private readonly ILogger<PortadorController> _logger;

        public PortadorController(IPortadorService portadorService, ILogger<PortadorController> logger)
        {
            _portadorService = portadorService;
            _logger = logger;
        }

        /// <summary>
        /// Cria um novo portador
        /// </summary>
        /// <param name="createPortadorDto">Dados do portador a ser criado</param>
        /// <returns>Portador criado</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOrOperador")] // Admins e Operadores podem criar portadors
        public async Task<ActionResult<PortadorDto>> Create([FromBody] CreatePortadorDto createPortadorDto)
        {
            _logger.LogInformation("Requisição para criar portador. Nome: {Nome}, Tipo: {Tipo}", 
                createPortadorDto.Nome, createPortadorDto.Tipo);

            try
            {
                var portador = await _portadorService.CreateAsync(createPortadorDto);
                return CreatedAtAction(nameof(GetById), new { id = portador.Id }, portador);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar portador");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Busca um portador por ID
        /// </summary>
        /// <param name="id">ID do portador</param>
        /// <returns>Portador encontrado</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<PortadorDto>> GetById(string id)
        {
            _logger.LogInformation("Requisição para buscar portador por ID: {Id}", id);

            try
            {
                var portador = await _portadorService.GetByIdAsync(id);
                
                if (portador == null)
                {
                    return NotFound(new { message = "Portador não encontrado" });
                }

                return Ok(portador);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar portador por ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Lista todos os portadors ativos
        /// </summary>
        /// <returns>Lista de portadors ativos</returns>
        [HttpGet]
        [Authorize(Policy = "TodosFuncionarios")] // Todos os funcionários podem listar portadors
        public async Task<ActionResult<IEnumerable<PortadorDto>>> GetAll()
        {
            _logger.LogInformation("Requisição para listar todos os portadors ativos");

            try
            {
                var portadors = await _portadorService.GetAllAsync();
                return Ok(portadors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar portadors");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza um portador existente
        /// </summary>
        /// <param name="id">ID do portador</param>
        /// <param name="updatePortadorDto">Dados atualizados do portador</param>
        /// <returns>Portador atualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem atualizar portadors
        public async Task<ActionResult<PortadorDto>> Update(string id, [FromBody] CreatePortadorDto updatePortadorDto)
        {
            _logger.LogInformation("Requisição para atualizar portador. ID: {Id}, Nome: {Nome}", 
                id, updatePortadorDto.Nome);

            try
            {
                var portador = await _portadorService.UpdateAsync(id, updatePortadorDto);
                return Ok(portador);
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
                _logger.LogError(ex, "Erro ao atualizar portador. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Desativa um portador (exclusão lógica)
        /// </summary>
        /// <param name="id">ID do portador</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem desativar portadors
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation("Requisição para desativar portador. ID: {Id}", id);

            try
            {
                var resultado = await _portadorService.DeleteAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Portador não encontrado" });
                }

                return Ok(new { message = "Portador desativado com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar portador. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Reativa um portador
        /// </summary>
        /// <param name="id">ID do portador</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("{id}/ativar")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem ativar portadors
        public async Task<ActionResult> Activate(string id)
        {
            _logger.LogInformation("Requisição para ativar portador. ID: {Id}", id);

            try
            {
                var resultado = await _portadorService.ActivateAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Portador não encontrado" });
                }

                return Ok(new { message = "Portador ativado com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar portador. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }
    }
}