using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface IPortadorRepository
    {
        Task<Portador> CreateAsync(Portador portador);
        Task<Portador?> GetByIdAsync(string id);
        Task<Portador?> GetByCardUidAsync(string cardUid);
        Task<IEnumerable<Portador>> GetAllAsync();
        Task<Portador> UpdateAsync(Portador portador);
        Task<bool> DeleteAsync(string id);
    }
}