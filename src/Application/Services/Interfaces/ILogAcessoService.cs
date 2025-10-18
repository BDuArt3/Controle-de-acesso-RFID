using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface ILogAcessoService
    {
        Task<LogAcessoDto> RegistrarTentativaAsync(CreateLogAcessoDto createLogAcessoDto);
        Task<LogAcessoDto?> GetByIdAsync(long id);
        Task<IEnumerable<LogAcessoDto>> GetAllAsync();
        Task<IEnumerable<LogAcessoDto>> GetByCartaoUidAsync(string cartaoUid);
        Task<IEnumerable<LogAcessoDto>> GetByDataPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<LogAcessoDto>> GetAcessosPermitidosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<LogAcessoDto>> GetAcessosNegadosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<LogAcessoDto>> GetUltimosAcessosAsync(int quantidade = 50);
        Task<object> GetEstatisticasHojeAsync();
        Task<object> GetRelatorioAcessosDiarioAsync(DateTime data);
    }
}