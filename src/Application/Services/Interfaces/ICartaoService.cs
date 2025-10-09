using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface ICartaoService
    {
        Task<CartaoDto> CreateAsync(CreateCartaoDto createCartaoDto);
        Task<CartaoDto?> GetByIdAsync(string id);
        Task<CartaoDto?> GetByUidAsync(string uid);
        Task<IEnumerable<CartaoDto>> GetAllAsync();
        Task<IEnumerable<CartaoDto>> GetByUsuarioIdAsync(string usuarioId);
        Task<CartaoDto> UpdateAsync(string id, CreateCartaoDto updateCartaoDto);
        Task<bool> DeleteAsync(string id);
        Task<bool> ActivateAsync(string id);
    }
}