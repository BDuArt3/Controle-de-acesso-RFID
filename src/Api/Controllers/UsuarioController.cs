using Microsoft.AspNetCore.Mvc;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        /// <summary>
        /// Cria um novo usuário
        /// </summary>
        /// <param name="createUsuarioDto">Dados do usuário a ser criado</param>
        /// <returns>Usuário criado</returns>
        [HttpPost]
        public async Task<ActionResult<UsuarioDto>> Create([FromBody] CreateUsuarioDto createUsuarioDto)
        {
            _logger.LogInformation("Requisição para criar usuário. Nome: {Nome}, Tipo: {Tipo}", 
                createUsuarioDto.Nome, createUsuarioDto.Tipo);

            try
            {
                var usuario = await _usuarioService.CreateAsync(createUsuarioDto);
                return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Busca um usuário por ID
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Usuário encontrado</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> GetById(string id)
        {
            _logger.LogInformation("Requisição para buscar usuário por ID: {Id}", id);

            try
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                
                if (usuario == null)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(usuario);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Lista todos os usuários ativos
        /// </summary>
        /// <returns>Lista de usuários ativos</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll()
        {
            _logger.LogInformation("Requisição para listar todos os usuários ativos");

            try
            {
                var usuarios = await _usuarioService.GetAllAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar usuários");
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza um usuário existente
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <param name="updateUsuarioDto">Dados atualizados do usuário</param>
        /// <returns>Usuário atualizado</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<UsuarioDto>> Update(string id, [FromBody] CreateUsuarioDto updateUsuarioDto)
        {
            _logger.LogInformation("Requisição para atualizar usuário. ID: {Id}, Nome: {Nome}", 
                id, updateUsuarioDto.Nome);

            try
            {
                var usuario = await _usuarioService.UpdateAsync(id, updateUsuarioDto);
                return Ok(usuario);
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
                _logger.LogError(ex, "Erro ao atualizar usuário. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Desativa um usuário (exclusão lógica)
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation("Requisição para desativar usuário. ID: {Id}", id);

            try
            {
                var resultado = await _usuarioService.DeleteAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(new { message = "Usuário desativado com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar usuário. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Reativa um usuário
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Resultado da operação</returns>
        [HttpPut("{id}/ativar")]
        public async Task<ActionResult> Activate(string id)
        {
            _logger.LogInformation("Requisição para ativar usuário. ID: {Id}", id);

            try
            {
                var resultado = await _usuarioService.ActivateAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(new { message = "Usuário ativado com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar usuário. ID: {Id}", id);
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }
    }
}