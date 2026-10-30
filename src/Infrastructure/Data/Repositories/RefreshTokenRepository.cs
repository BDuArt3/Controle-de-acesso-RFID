using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControleDeAcesso.Infrastructure.Data.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _context.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryRotateAsync(
        string currentTokenHash,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var updated = await _context.RefreshTokens
                .Where(token =>
                    token.TokenHash == currentTokenHash &&
                    token.RevokedAtUtc == null &&
                    token.ExpiresAtUtc > nowUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(token => token.RevokedAtUtc, nowUtc)
                        .SetProperty(token => token.ReplacedByTokenHash, replacement.TokenHash),
                    cancellationToken);

            if (updated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _context.RefreshTokens.Add(replacement);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> RevokeAsync(
        string tokenHash,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await _context.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);
        return updated == 1;
    }

    public Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens
            .Where(token => token.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
}
