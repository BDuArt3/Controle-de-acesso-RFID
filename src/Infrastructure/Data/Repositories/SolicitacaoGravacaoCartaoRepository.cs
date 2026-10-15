using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public sealed class SolicitacaoGravacaoCartaoRepository : ISolicitacaoGravacaoCartaoRepository
    {
        private readonly AppDbContext _context;

        public SolicitacaoGravacaoCartaoRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<SolicitacaoGravacaoCartao?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken) =>
            _context.SolicitacoesGravacaoCartao
                .FirstOrDefaultAsync(solicitacao => solicitacao.Id == id, cancellationToken);

        public Task<SolicitacaoGravacaoCartao?> GetByIdempotencyKeyAsync(
            string funcionarioId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            _context.SolicitacoesGravacaoCartao
                .FirstOrDefaultAsync(
                    solicitacao => solicitacao.SolicitanteFuncionarioId == funcionarioId &&
                                   solicitacao.ChaveIdempotencia == idempotencyKey,
                    cancellationToken);

        public Task<SolicitacaoGravacaoCartao?> GetByCredentialAsync(
            string credential,
            CancellationToken cancellationToken) =>
            _context.SolicitacoesGravacaoCartao
                .FirstOrDefaultAsync(
                    solicitacao => solicitacao.Credencial == credential,
                    cancellationToken);

        public Task<bool> CredentialExistsAsync(string credential, CancellationToken cancellationToken) =>
            _context.SolicitacoesGravacaoCartao
                .AnyAsync(solicitacao => solicitacao.Credencial == credential, cancellationToken);

        public Task<bool> HasOpenRequestForCardAsync(
            string cartaoId,
            CancellationToken cancellationToken) =>
            _context.SolicitacoesGravacaoCartao
                .AnyAsync(
                    solicitacao => solicitacao.CartaoAnteriorId == cartaoId &&
                                   solicitacao.Status != StatusSolicitacaoGravacao.Concluida &&
                                   solicitacao.Status != StatusSolicitacaoGravacao.Cancelada,
                    cancellationToken);

        public async Task AddAsync(
            SolicitacaoGravacaoCartao solicitacao,
            CancellationToken cancellationToken)
        {
            await _context.SolicitacoesGravacaoCartao.AddAsync(solicitacao, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}
