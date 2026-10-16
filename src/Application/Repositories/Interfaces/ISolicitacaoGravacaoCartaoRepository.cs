using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface ISolicitacaoGravacaoCartaoRepository
    {
        Task<SolicitacaoGravacaoCartao?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<SolicitacaoGravacaoCartao?> GetByIdempotencyKeyAsync(
            string funcionarioId,
            string idempotencyKey,
            CancellationToken cancellationToken);
        Task<SolicitacaoGravacaoCartao?> GetByCredentialAsync(
            string credential,
            CancellationToken cancellationToken);
        Task<bool> CredentialExistsAsync(string credential, CancellationToken cancellationToken);
        Task<bool> HasOpenRequestForCardAsync(string cartaoId, CancellationToken cancellationToken);
        Task AddAsync(SolicitacaoGravacaoCartao solicitacao, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
