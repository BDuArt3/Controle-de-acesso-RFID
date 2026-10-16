using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface ILogAcessoRepository
    {
        Task<LogAcesso> CreateAsync(LogAcesso logAcesso);
        Task<LogAcesso?> GetByIdAsync(long id);
        Task<IEnumerable<LogAcesso>> GetAllAsync();
        Task<IEnumerable<LogAcesso>> GetByCartaoIdAsync(string cartaoId);
        Task<IEnumerable<LogAcesso>> GetByUidInformadoAsync(string uidInformado);
        Task<IEnumerable<LogAcesso>> GetByDataPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<LogAcesso>> GetByDataPeriodoAndStatusAsync(DateTime dataInicio, DateTime dataFim, bool acessoPermitido);
        Task<IEnumerable<LogAcesso>> GetUltimosAcessosAsync(int quantidade);
        Task<int> GetTotalAcessosHojeAsync();
        Task<int> GetTotalAcessosPermitidosHojeAsync();
        Task<int> GetTotalAcessosNegadosHojeAsync();
    }
}