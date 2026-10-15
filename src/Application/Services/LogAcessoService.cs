using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class LogAcessoService : ILogAcessoService
    {
        private readonly ILogAcessoRepository _logAcessoRepository;
        private readonly ICartaoRepository _cartaoRepository;
        private readonly ILogger<LogAcessoService> _logger;

        public LogAcessoService(
            ILogAcessoRepository logAcessoRepository,
            ICartaoRepository cartaoRepository,
            ILogger<LogAcessoService> logger)
        {
            _logAcessoRepository = logAcessoRepository;
            _cartaoRepository = cartaoRepository;
            _logger = logger;
        }

        public async Task<LogAcessoDto> RegistrarTentativaAsync(CreateLogAcessoDto createLogAcessoDto)
        {
            _logger.LogInformation("Registrando tentativa de acesso. Permitido: {AcessoPermitido}",
                createLogAcessoDto.AcessoPermitido);

            try
            {
                string? cartaoId = null;
                string? usuarioId = null;
                string? uidInformado = null;
                Cartao? cartaoParaLog = null;

                if (!string.IsNullOrWhiteSpace(createLogAcessoDto.CartaoId) ||
                    !string.IsNullOrWhiteSpace(createLogAcessoDto.UsuarioId))
                {
                    if (string.IsNullOrWhiteSpace(createLogAcessoDto.CartaoId) ||
                        string.IsNullOrWhiteSpace(createLogAcessoDto.UsuarioId))
                    {
                        throw new ArgumentException("CartaoId e UsuarioId devem ser informados juntos.");
                    }

                    cartaoParaLog = await _cartaoRepository.GetByIdAsync(createLogAcessoDto.CartaoId);
                    if (cartaoParaLog is null || cartaoParaLog.UsuarioId != createLogAcessoDto.UsuarioId)
                    {
                        throw new ArgumentException("Cartão e usuário do log não correspondem.");
                    }

                    cartaoId = cartaoParaLog.Id;
                    usuarioId = cartaoParaLog.UsuarioId;
                }
                else if (!string.IsNullOrWhiteSpace(createLogAcessoDto.UidInformado))
                {
                    uidInformado = createLogAcessoDto.UidInformado.Trim();
                }
                else
                {
                    var uid = createLogAcessoDto.CartaoUid;
                    if (!string.IsNullOrWhiteSpace(uid))
                    {
                        cartaoParaLog = await _cartaoRepository.GetByUidAsync(uid.Trim());
                        if (cartaoParaLog is null)
                        {
                            uidInformado = uid.Trim();
                        }
                        else
                        {
                            cartaoId = cartaoParaLog.Id;
                            usuarioId = cartaoParaLog.UsuarioId;
                        }
                    }
                }

                var logAcesso = new LogAcesso
                {
                    CartaoId = cartaoId,
                    Cartao = cartaoParaLog,
                    UsuarioId = usuarioId,
                    Usuario = cartaoParaLog?.Usuario,
                    UidInformado = uidInformado,
                    AcessoPermitido = createLogAcessoDto.AcessoPermitido,
                    Motivo = createLogAcessoDto.Motivo?.Trim() ?? string.Empty,
                    MotivoTipo = createLogAcessoDto.MotivoTipo,
                    DataHora = DateTime.UtcNow
                };

                var logCriado = await _logAcessoRepository.CreateAsync(logAcesso);

                _logger.LogInformation("Tentativa de acesso registrada com sucesso. ID: {Id}", logCriado.Id);

                return await MapToDtoWithUserInfoAsync(logCriado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar tentativa de acesso.");
                throw;
            }
        }

        public async Task<LogAcessoDto?> GetByIdAsync(long id)
        {
            _logger.LogInformation("Buscando log de acesso por ID: {Id}", id);

            try
            {
                if (id <= 0)
                {
                    throw new ArgumentException("ID deve ser maior que zero", nameof(id));
                }

                var log = await _logAcessoRepository.GetByIdAsync(id);

                if (log == null)
                {
                    _logger.LogWarning("Log de acesso não encontrado para ID: {Id}", id);
                    return null;
                }

                return await MapToDtoWithUserInfoAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar log de acesso por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os logs de acesso");

            try
            {
                var logs = await _logAcessoRepository.GetAllAsync();
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                _logger.LogInformation("Retornados {Count} logs de acesso", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os logs de acesso");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetByCartaoUidAsync(string cartaoUid)
        {
            _logger.LogInformation("Buscando logs de acesso por CartaoUid: {CartaoUid}", cartaoUid);

            try
            {
                if (string.IsNullOrWhiteSpace(cartaoUid))
                {
                    throw new ArgumentException("UID do cartão é obrigatório", nameof(cartaoUid));
                }

                var cartao = await _cartaoRepository.GetByUidAsync(cartaoUid.Trim());
                var logs = cartao is null
                    ? await _logAcessoRepository.GetByUidInformadoAsync(cartaoUid.Trim())
                    : await _logAcessoRepository.GetByCartaoIdAsync(cartao.Id);
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por CartaoUid: {CartaoUid}", cartaoUid);
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetByDataPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            _logger.LogInformation("Buscando logs por período: {DataInicio} - {DataFim}", dataInicio, dataFim);

            try
            {
                if (dataInicio > dataFim)
                {
                    throw new ArgumentException("Data início deve ser menor ou igual à data fim");
                }

                var logs = await _logAcessoRepository.GetByDataPeriodoAsync(
                    ToUtc(dataInicio), ToUtc(dataFim));
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por período");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetAcessosPermitidosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            _logger.LogInformation("Buscando acessos permitidos por período: {DataInicio} - {DataFim}", dataInicio, dataFim);

            try
            {
                var logs = await _logAcessoRepository.GetByDataPeriodoAndStatusAsync(
                    ToUtc(dataInicio), ToUtc(dataFim), true);
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar acessos permitidos por período");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetAcessosNegadosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            _logger.LogInformation("Buscando acessos negados por período: {DataInicio} - {DataFim}", dataInicio, dataFim);

            try
            {
                var logs = await _logAcessoRepository.GetByDataPeriodoAndStatusAsync(
                    ToUtc(dataInicio), ToUtc(dataFim), false);
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar acessos negados por período");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcessoDto>> GetUltimosAcessosAsync(int quantidade = 50)
        {
            _logger.LogInformation("Buscando últimos {Quantidade} acessos", quantidade);

            try
            {
                if (quantidade <= 0)
                {
                    quantidade = 50;
                }

                var logs = await _logAcessoRepository.GetUltimosAcessosAsync(quantidade);
                var result = new List<LogAcessoDto>();

                foreach (var log in logs)
                {
                    result.Add(await MapToDtoWithUserInfoAsync(log));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar últimos acessos");
                throw;
            }
        }

        public async Task<object> GetEstatisticasHojeAsync()
        {
            _logger.LogInformation("Gerando estatísticas de acesso de hoje");

            try
            {
                var totalHoje = await _logAcessoRepository.GetTotalAcessosHojeAsync();
                var permitidosHoje = await _logAcessoRepository.GetTotalAcessosPermitidosHojeAsync();
                var negadosHoje = await _logAcessoRepository.GetTotalAcessosNegadosHojeAsync();

                var estatisticas = new
                {
                    Data = DateTime.UtcNow.Date,
                    TotalTentativas = totalHoje,
                    AcessosPermitidos = permitidosHoje,
                    AcessosNegados = negadosHoje,
                    PercentualSucesso = totalHoje > 0 ? (permitidosHoje * 100.0 / totalHoje) : 0,
                    PercentualFalhas = totalHoje > 0 ? (negadosHoje * 100.0 / totalHoje) : 0
                };

                _logger.LogInformation("Estatísticas geradas: Total: {Total}, Permitidos: {Permitidos}, Negados: {Negados}", 
                    totalHoje, permitidosHoje, negadosHoje);

                return estatisticas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar estatísticas de hoje");
                throw;
            }
        }

        public async Task<object> GetRelatorioAcessosDiarioAsync(DateTime data)
        {
            _logger.LogInformation("Gerando relatório de acessos para data: {Data}", data);

            try
            {
                var dataInicio = ToUtc(data).Date;
                var dataFim = dataInicio.AddDays(1);

                var logs = (await _logAcessoRepository.GetByDataPeriodoAsync(dataInicio, dataFim.AddTicks(-1))).ToList();
                var permitidos = logs.Count(l => l.AcessoPermitido);
                var negados = logs.Count(l => !l.AcessoPermitido);

                var relatorio = new
                {
                    Data = dataInicio,
                    TotalTentativas = logs.Count(),
                    AcessosPermitidos = permitidos,
                    AcessosNegados = negados,
                    PercentualSucesso = logs.Count() > 0 ? (permitidos * 100.0 / logs.Count()) : 0,
                    HorarioPico = logs.GroupBy(l => l.DataHora.Hour)
                                     .OrderByDescending(g => g.Count())
                                     .FirstOrDefault()?.Key ?? 0,
                    CartoesMaisUsados = logs.GroupBy(l => l.CartaoId ?? $"unknown:{l.UidInformado ?? "empty"}")
                                          .Select(g => new
                                          {
                                              CartaoId = g.First().CartaoId,
                                              CartaoUid = g.First().Cartao?.Uid ?? g.First().UidInformado ?? string.Empty,
                                              Tentativas = g.Count()
                                          })
                                          .OrderByDescending(x => x.Tentativas)
                                          .Take(5)
                                          .ToList()
                };

                return relatorio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório diário para data: {Data}", data);
                throw;
            }
        }

        private async Task<LogAcessoDto> MapToDtoWithUserInfoAsync(LogAcesso logAcesso)
        {
            var dto = new LogAcessoDto
            {
                Id = logAcesso.Id,
                DataHora = DateTime.SpecifyKind(logAcesso.DataHora, DateTimeKind.Utc),
                CartaoId = logAcesso.CartaoId,
                UsuarioId = logAcesso.UsuarioId,
                UidInformado = logAcesso.UidInformado,
                CartaoUid = logAcesso.CartaoId is not null
                    ? logAcesso.Cartao?.Uid ?? string.Empty
                    : logAcesso.UidInformado ?? string.Empty,
                AcessoPermitido = logAcesso.AcessoPermitido,
                Motivo = logAcesso.Motivo,
                MotivoTipo = logAcesso.MotivoTipo,
                UsuarioNome = logAcesso.Usuario?.Nome,
                UsuarioTipo = logAcesso.Usuario?.Tipo.ToString()
            };

            return await Task.FromResult(dto);
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}