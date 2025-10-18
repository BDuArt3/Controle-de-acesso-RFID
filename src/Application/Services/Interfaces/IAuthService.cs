using ControleDeAcesso.Application.DTOs;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<TokenDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateAccessToken(FuncionarioDto funcionario);
        string GenerateRefreshToken();
    }
}