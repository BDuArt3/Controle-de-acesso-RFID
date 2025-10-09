using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface ICartaoRepository
    {
        Task<Cartao> CreateAsync(Cartao cartao);
        Task<Cartao?> GetByIdAsync(string id);
        Task<Cartao?> GetByUidAsync(string uid);
        Task<IEnumerable<Cartao>> GetAllAsync();
        Task<IEnumerable<Cartao>> GetByPortadorIdAsync(string portadorId);
        Task<Cartao> UpdateAsync(Cartao cartao);
        Task<bool> DeleteAsync(string id);
        Task<bool> UidExistsAsync(string uid);
    }
}