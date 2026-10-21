using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IFuncionarioService
    {
        Task<FuncionarioDto> CreateAsync(CreateFuncionarioDto funcionarioDto);
        Task<FuncionarioDto?> GetByIdAsync(string id);
        Task<IEnumerable<FuncionarioListDto>> GetAllAsync();
        Task<FuncionarioDto> UpdateAsync(string id, CreateFuncionarioDto funcionarioDto);
        Task<bool> DeleteAsync(string id, string funcionarioResponsavelId, string motivo);
        Task<bool> ActivateAsync(string id, string funcionarioResponsavelId, string motivo);
    }
}
