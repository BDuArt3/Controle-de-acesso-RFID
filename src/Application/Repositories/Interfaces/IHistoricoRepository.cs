using ControleDeAcesso.Application.DTOs;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    // Retornam null quando a entidade consultada não existe.
    public interface IHistoricoRepository
    {
        Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetStatusUsuarioAsync(string usuarioId, int page, int pageSize);
        Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetStatusFuncionarioAsync(string funcionarioId, int page, int pageSize);
        Task<HistoricoCartaoDto?> GetCartaoAsync(string cartaoId, int page, int pageSize);
    }
}
