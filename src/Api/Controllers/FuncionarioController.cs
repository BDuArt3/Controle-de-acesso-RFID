using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos os endpoints requerem autenticação
    public class FuncionarioController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;

        public FuncionarioController(IFuncionarioService funcionarioService)
        {
            _funcionarioService = funcionarioService;
        }

        /// <summary>
        /// Cria um novo funcionário
        /// </summary>
        /// <param name="funcionarioDto">Dados do funcionário a ser criado</param>
        /// <returns>Funcionário criado</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem criar funcionários
        public async Task<IActionResult> Create([FromBody] CreateFuncionarioDto funcionarioDto)
        {
            try
            {
                var funcionario = await _funcionarioService.CreateAsync(funcionarioDto);
                return CreatedAtAction(nameof(GetById), new { id = funcionario.Id }, funcionario);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Busca um funcionário por ID
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>Funcionário encontrado</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { message = "ID do funcionário é obrigatório" });
                }

                var funcionario = await _funcionarioService.GetByIdAsync(id);
                
                if (funcionario == null)
                {
                    return NotFound(new { message = "Funcionário não encontrado" });
                }

                return Ok(funcionario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Lista todos os funcionários ativos
        /// </summary>
        /// <returns>Lista de funcionários</returns>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem listar funcionários
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var funcionarios = await _funcionarioService.GetAllAsync();
                return Ok(funcionarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza um funcionário existente
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <param name="funcionarioDto">Dados atualizados do funcionário</param>
        /// <returns>Funcionário atualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem atualizar funcionários
        public async Task<IActionResult> Update(string id, [FromBody] CreateFuncionarioDto funcionarioDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { message = "ID do funcionário é obrigatório" });
                }

                var funcionario = await _funcionarioService.UpdateAsync(id, funcionarioDto);
                return Ok(funcionario);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Desativa um funcionário (soft delete)
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>Confirmação da operação</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem desativar funcionários
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { message = "ID do funcionário é obrigatório" });
                }

                var resultado = await _funcionarioService.DeleteAsync(id);
                
                if (!resultado)
                {
                    return NotFound(new { message = "Funcionário não encontrado" });
                }

                return Ok(new { message = "Funcionário desativado com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }

        /// <summary>
        /// Ativa um funcionário previamente desativado
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <returns>Confirmação da operação</returns>
        [HttpPatch("{id}/ativar")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem ativar funcionários
        public async Task<IActionResult> Ativar(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { message = "ID do funcionário é obrigatório" });
                }

                var funcionario = await _funcionarioService.GetByIdAsync(id);
                
                if (funcionario == null)
                {
                    return NotFound(new { message = "Funcionário não encontrado" });
                }

                // Criar um DTO temporário para ativar o funcionário
                var updateDto = new CreateFuncionarioDto
                {
                    NomeFuncionario = funcionario.NomeFuncionario,
                    UsuarioAcesso = funcionario.UsuarioAcesso,
                    Senha = "", // Senha vazia para não alterar
                    Tipo = funcionario.Tipo
                };

                var funcionarioAtualizado = await _funcionarioService.UpdateAsync(id, updateDto);
                return Ok(new { message = "Funcionário ativado com sucesso", funcionario = funcionarioAtualizado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
            }
        }
    }
}