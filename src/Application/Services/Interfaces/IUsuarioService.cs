using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<UsuarioDto> CreateAsync(CreateUsuarioDto createUsuarioDto);
        Task<UsuarioDto?> GetByIdAsync(string id);
        Task<IEnumerable<UsuarioDto>> GetAllAsync();
        Task<UsuarioDto> UpdateAsync(string id, CreateUsuarioDto updateUsuarioDto);
        Task<bool> DeleteAsync(string id);
        Task<bool> ActivateAsync(string id);
    }
}