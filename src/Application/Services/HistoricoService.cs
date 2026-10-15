using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services.Interfaces;

namespace ControleDeAcesso.Application.Services
{
    public class HistoricoService : IHistoricoService
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        private readonly IHistoricoRepository _repository;

        public HistoricoService(IHistoricoRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetUsuarioAsync(string usuarioId, int page, int pageSize)
        {
            RequireId(usuarioId, nameof(usuarioId));
            (page, pageSize) = Normalize(page, pageSize);
            return _repository.GetStatusUsuarioAsync(usuarioId, page, pageSize);
        }

        public Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetFuncionarioAsync(string funcionarioId, int page, int pageSize)
        {
            RequireId(funcionarioId, nameof(funcionarioId));
            (page, pageSize) = Normalize(page, pageSize);
            return _repository.GetStatusFuncionarioAsync(funcionarioId, page, pageSize);
        }

        public Task<HistoricoCartaoDto?> GetCartaoAsync(string cartaoId, int page, int pageSize)
        {
            RequireId(cartaoId, nameof(cartaoId));
            (page, pageSize) = Normalize(page, pageSize);
            return _repository.GetCartaoAsync(cartaoId, page, pageSize);
        }

        private static void RequireId(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID é obrigatório.", name);
            }
        }

        private static (int Page, int PageSize) Normalize(int page, int pageSize)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
            return (normalizedPage, normalizedSize);
        }
    }
}
