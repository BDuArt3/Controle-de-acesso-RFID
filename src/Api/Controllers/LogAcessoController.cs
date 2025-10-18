using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requer autenticação para todos os endpoints
    public class LogAcessoController : ControllerBase
    {
        private readonly ILogAcessoService _logAcessoService;
        private readonly ILogger<LogAcessoController> _logger;

        public LogAcessoController(ILogAcessoService logAcessoService, ILogger<LogAcessoController> logger)
        {
            _logAcessoService = logAcessoService;
            _logger = logger;
        }

        /// <summary>
        /// Registra uma tentativa de acesso no sistema
        /// </summary>
        /// <param name="createLogAcessoDto">Dados da tentativa de acesso</param>
        /// <returns>Log de acesso criado</returns>
        [HttpPost]
        [ProducesResponseType(typeof(LogAcessoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LogAcessoDto>> RegistrarTentativa([FromBody] CreateLogAcessoDto createLogAcessoDto)
        {
            try
            {
                _logger.LogInformation("Recebida solicitação para registrar tentativa de acesso");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var log = await _logAcessoService.RegistrarTentativaAsync(createLogAcessoDto);
                return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Dados inválidos para registrar tentativa de acesso");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar tentativa de acesso");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém um log de acesso específico pelo ID
        /// </summary>
        /// <param name="id">ID do log de acesso</param>
        /// <returns>Log de acesso encontrado</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem visualizar logs específicos
        [ProducesResponseType(typeof(LogAcessoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LogAcessoDto>> GetById(long id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("ID deve ser um valor positivo");
                }

                var log = await _logAcessoService.GetByIdAsync(id);

                if (log == null)
                {
                    return NotFound($"Log de acesso com ID {id} não encontrado");
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar log de acesso por ID: {Id}", id);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém todos os logs de acesso
        /// </summary>
        /// <returns>Lista de logs de acesso</returns>
        [HttpGet]
        [Authorize(Policy = "TodosFuncionarios")] // Todos os funcionários podem visualizar logs
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetAll()
        {
            try
            {
                var logs = await _logAcessoService.GetAllAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os logs de acesso");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém logs de acesso por UID do cartão
        /// </summary>
        /// <param name="uid">UID do cartão</param>
        /// <returns>Lista de logs do cartão</returns>
        [HttpGet("cartao/{uid}")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem buscar logs por cartão
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetByCartaoUid(string uid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uid))
                {
                    return BadRequest("UID do cartão é obrigatório");
                }

                var logs = await _logAcessoService.GetByCartaoUidAsync(uid);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por UID do cartão: {Uid}", uid);
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém logs de acesso por período
        /// </summary>
        /// <param name="dataInicio">Data de início (yyyy-MM-dd)</param>
        /// <param name="dataFim">Data de fim (yyyy-MM-dd)</param>
        /// <returns>Lista de logs do período</returns>
        [HttpGet("periodo")]
        [Authorize(Policy = "AdminOnly")] // Apenas admins podem buscar logs por período
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetByPeriodo(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            try
            {
                if (dataInicio > dataFim)
                {
                    return BadRequest("Data de início deve ser menor ou igual à data de fim");
                }

                var logs = await _logAcessoService.GetByDataPeriodoAsync(dataInicio, dataFim);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por período");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém apenas acessos permitidos por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <returns>Lista de acessos permitidos</returns>
        [HttpGet("periodo/permitidos")]
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetAcessosPermitidos(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            try
            {
                var logs = await _logAcessoService.GetAcessosPermitidosPorPeriodoAsync(dataInicio, dataFim);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar acessos permitidos por período");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém apenas acessos negados por período
        /// </summary>
        /// <param name="dataInicio">Data de início</param>
        /// <param name="dataFim">Data de fim</param>
        /// <returns>Lista de acessos negados</returns>
        [HttpGet("periodo/negados")]
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetAcessosNegados(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            try
            {
                var logs = await _logAcessoService.GetAcessosNegadosPorPeriodoAsync(dataInicio, dataFim);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar acessos negados por período");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém os últimos acessos registrados
        /// </summary>
        /// <param name="quantidade">Quantidade de registros (padrão: 50)</param>
        /// <returns>Lista dos últimos acessos</returns>
        [HttpGet("ultimos")]
        [ProducesResponseType(typeof(IEnumerable<LogAcessoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<LogAcessoDto>>> GetUltimos(
            [FromQuery] int quantidade = 50)
        {
            try
            {
                var logs = await _logAcessoService.GetUltimosAcessosAsync(quantidade);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar últimos acessos");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém estatísticas de acesso do dia atual
        /// </summary>
        /// <returns>Estatísticas de hoje</returns>
        [HttpGet("estatisticas/hoje")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetEstatisticasHoje()
        {
            try
            {
                var estatisticas = await _logAcessoService.GetEstatisticasHojeAsync();
                return Ok(estatisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar estatísticas de hoje");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Obtém relatório completo de acessos para uma data específica
        /// </summary>
        /// <param name="data">Data do relatório (yyyy-MM-dd)</param>
        /// <returns>Relatório detalhado do dia</returns>
        [HttpGet("relatorio/diario")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetRelatorioDiario([FromQuery] DateTime data)
        {
            try
            {
                var relatorio = await _logAcessoService.GetRelatorioAcessosDiarioAsync(data);
                return Ok(relatorio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório diário para data: {Data}", data);
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }
}