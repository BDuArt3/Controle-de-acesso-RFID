using ControleDeAcesso.Application.DTOs;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IHistoricoService
    {
        Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetUsuarioAsync(string usuarioId, int page, int pageSize);
        Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetFuncionarioAsync(string funcionarioId, int page, int pageSize);
        Task<HistoricoCartaoDto?> GetCartaoAsync(string cartaoId, int page, int pageSize);
    }
}
