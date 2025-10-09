using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IPortadorService
    {
        Task<PortadorDto> CreateAsync(CreatePortadorDto createPortadorDto);
        Task<PortadorDto?> GetByIdAsync(string id);
        Task<IEnumerable<PortadorDto>> GetAllAsync();
        Task<PortadorDto> UpdateAsync(string id, CreatePortadorDto updatePortadorDto);
        Task<bool> DeleteAsync(string id);
        Task<bool> ActivateAsync(string id);
    }
}