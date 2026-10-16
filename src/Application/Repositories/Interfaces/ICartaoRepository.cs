using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface ICartaoRepository
    {
        Task<Cartao> CreateWithAuditAsync(
            Cartao cartao,
            HistoricoAssociacoesCartao historicoAssociacao,
            HistoricoStatusCartao historicoStatus);
        Task<Cartao?> GetByIdAsync(string id);
        Task<Cartao?> GetByUidAsync(string uid);
        Task<IEnumerable<Cartao>> GetAllAsync();
        Task<IEnumerable<Cartao>> GetByUsuarioIdAsync(string usuarioId);
        Task<IEnumerable<Cartao>> GetAllByUsuarioIdAsync(string usuarioId);
        Task<Cartao> UpdateStatusWithHistoryAsync(Cartao cartao, HistoricoStatusCartao historicoStatus);
        Task<bool> UidExistsAsync(string uid);
    }
}