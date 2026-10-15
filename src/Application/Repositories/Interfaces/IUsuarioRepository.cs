using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario?> GetByIdAsync(string id);
        Task<Usuario?> GetByCardUidAsync(string cardUid);
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<bool> EmailExistsAsync(string email, string? excludingUsuarioId = null);
        Task<Usuario> UpdateAsync(Usuario usuario);
        Task<bool> DeleteAsync(string id);
    }
}