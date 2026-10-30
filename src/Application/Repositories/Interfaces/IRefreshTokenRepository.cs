using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> TryRotateAsync(
        string currentTokenHash,
        RefreshToken replacement,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string tokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
