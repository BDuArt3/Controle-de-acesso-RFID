using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class LogAcessoRepository : ILogAcessoRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LogAcessoRepository> _logger;

        public LogAcessoRepository(AppDbContext context, ILogger<LogAcessoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LogAcesso> CreateAsync(LogAcesso logAcesso)
        {
            _logger.LogInformation("Registrando log de acesso. CartaoUid: {CartaoUid}, AcessoPermitido: {AcessoPermitido}", 
                logAcesso.CartaoUid, logAcesso.AcessoPermitido);

            try
            {
                logAcesso.DataHora = DateTime.Now;
                _context.LogsAcesso.Add(logAcesso);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Log de acesso registrado com sucesso. ID: {Id}, CartaoUid: {CartaoUid}", 
                    logAcesso.Id, logAcesso.CartaoUid);
                
                return logAcesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log de acesso. CartaoUid: {CartaoUid}", 
                    logAcesso.CartaoUid);
                throw;
            }
        }

        public async Task<LogAcesso?> GetByIdAsync(long id)
        {
            _logger.LogInformation("Buscando log de acesso por ID: {Id}", id);

            try
            {
                var log = await _context.LogsAcesso
                                 .FirstOrDefaultAsync(l => l.Id == id);

                if (log == null)
                {
                    _logger.LogWarning("Log de acesso não encontrado para ID: {Id}", id);
                }

                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar log de acesso por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<LogAcesso>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os logs de acesso");

            try
            {
                var logs = await _context.LogsAcesso
                                  .OrderByDescending(l => l.DataHora)
                                  .ToListAsync();

                _logger.LogInformation("Encontrados {Count} logs de acesso", logs.Count);

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os logs de acesso");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcesso>> GetByCartaoUidAsync(string cartaoUid)
        {
            _logger.LogInformation("Buscando logs de acesso por CartaoUid: {CartaoUid}", cartaoUid);

            try
            {
                var logs = await _context.LogsAcesso
                                  .Where(l => l.CartaoUid == cartaoUid)
                                  .OrderByDescending(l => l.DataHora)
                                  .ToListAsync();

                _logger.LogInformation("Encontrados {Count} logs para o cartão {CartaoUid}", 
                    logs.Count, cartaoUid);

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por CartaoUid: {CartaoUid}", cartaoUid);
                throw;
            }
        }

        public async Task<IEnumerable<LogAcesso>> GetByDataPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            _logger.LogInformation("Buscando logs de acesso por período: {DataInicio} - {DataFim}", 
                dataInicio, dataFim);

            try
            {
                var logs = await _context.LogsAcesso
                                  .Where(l => l.DataHora >= dataInicio && l.DataHora <= dataFim)
                                  .OrderByDescending(l => l.DataHora)
                                  .ToListAsync();

                _logger.LogInformation("Encontrados {Count} logs no período {DataInicio} - {DataFim}", 
                    logs.Count, dataInicio, dataFim);

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por período: {DataInicio} - {DataFim}", 
                    dataInicio, dataFim);
                throw;
            }
        }

        public async Task<IEnumerable<LogAcesso>> GetByDataPeriodoAndStatusAsync(DateTime dataInicio, DateTime dataFim, bool acessoPermitido)
        {
            _logger.LogInformation("Buscando logs de acesso por período e status: {DataInicio} - {DataFim}, Permitido: {AcessoPermitido}", 
                dataInicio, dataFim, acessoPermitido);

            try
            {
                var logs = await _context.LogsAcesso
                                  .Where(l => l.DataHora >= dataInicio && 
                                             l.DataHora <= dataFim && 
                                             l.AcessoPermitido == acessoPermitido)
                                  .OrderByDescending(l => l.DataHora)
                                  .ToListAsync();

                _logger.LogInformation("Encontrados {Count} logs no período com status {AcessoPermitido}", 
                    logs.Count, acessoPermitido);

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs por período e status");
                throw;
            }
        }

        public async Task<IEnumerable<LogAcesso>> GetUltimosAcessosAsync(int quantidade)
        {
            _logger.LogInformation("Buscando últimos {Quantidade} acessos", quantidade);

            try
            {
                var logs = await _context.LogsAcesso
                                  .OrderByDescending(l => l.DataHora)
                                  .Take(quantidade)
                                  .ToListAsync();

                _logger.LogInformation("Retornados {Count} últimos acessos", logs.Count);

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar últimos acessos");
                throw;
            }
        }

        public async Task<int> GetTotalAcessosHojeAsync()
        {
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            try
            {
                var total = await _context.LogsAcesso
                                   .CountAsync(l => l.DataHora >= hoje && l.DataHora < amanha);

                _logger.LogInformation("Total de acessos hoje: {Total}", total);

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar acessos de hoje");
                throw;
            }
        }

        public async Task<int> GetTotalAcessosPermitidosHojeAsync()
        {
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            try
            {
                var total = await _context.LogsAcesso
                                   .CountAsync(l => l.DataHora >= hoje && 
                                                   l.DataHora < amanha && 
                                                   l.AcessoPermitido == true);

                _logger.LogInformation("Total de acessos permitidos hoje: {Total}", total);

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar acessos permitidos de hoje");
                throw;
            }
        }

        public async Task<int> GetTotalAcessosNegadosHojeAsync()
        {
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            try
            {
                var total = await _context.LogsAcesso
                                   .CountAsync(l => l.DataHora >= hoje && 
                                                   l.DataHora < amanha && 
                                                   l.AcessoPermitido == false);

                _logger.LogInformation("Total de acessos negados hoje: {Total}", total);

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao contar acessos negados de hoje");
                throw;
            }
        }
    }
}