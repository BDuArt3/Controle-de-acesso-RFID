using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface IFuncionarioRepository
    {
        Task<Funcionario> CreateAsync(Funcionario funcionario);
        Task<Funcionario?> GetByIdAsync(string id);
        Task<Funcionario?> GetByEmailAsync(string usuarioAcesso);
        Task<IEnumerable<Funcionario>> GetAllAsync();
        Task<Funcionario> UpdateAsync(Funcionario funcionario);
        Task<bool> DeleteAsync(string id);
        Task<bool> EmailExistsAsync(string usuarioAcesso);
    }
}