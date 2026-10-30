using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ControleDeAcesso.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IFuncionarioRepository funcionarioRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _funcionarioRepository = funcionarioRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Dados de login são obrigatórios."
                };
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Senha))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Email e senha são obrigatórios."
                };
            }

            _logger.LogInformation("Tentativa de login para usuário: {Email}", loginDto.Email);

            try
            {
                // Buscar funcionário por usuário de acesso
                var funcionario = await _funcionarioRepository.GetByEmailAsync(loginDto.Email.Trim());

                if (funcionario == null)
                {
                    _logger.LogWarning("Usuário não encontrado: {Email}", loginDto.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Email ou senha inválidos."
                    };
                }

                // Login is valid only while both the account and its person record are active.
                if (!funcionario.IsActive || !funcionario.Usuario.IsActive)
                {
                    _logger.LogWarning("Tentativa de login com usuário inativo: {Email}", loginDto.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Usuário desativado."
                    };
                }

                // Verificar senha
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Senha, funcionario.SenhaHash))
                {
                    _logger.LogWarning("Senha incorreta para usuário: {Email}", loginDto.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Usuário ou senha inválidos."
                    };
                }

                // Converter entidade para DTO
                var funcionarioDto = new FuncionarioDto
                {
                    Id = funcionario.Id,
                    NomeFuncionario = funcionario.Usuario.Nome,
                    Email = funcionario.Usuario.Email ?? string.Empty,
                    Tipo = funcionario.Tipo,
                    IsActive = funcionario.IsActive
                };

                // Gerar tokens
                var accessToken = GenerateAccessToken(funcionarioDto);
                var refreshToken = GenerateRefreshToken();
                var now = DateTime.UtcNow;
                var expiresAt = now.AddDays(7);
                await _refreshTokenRepository.DeleteExpiredAsync(now);
                await _refreshTokenRepository.AddAsync(new RefreshToken
                {
                    TokenHash = HashRefreshToken(refreshToken),
                    FuncionarioId = funcionario.Id,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = expiresAt
                });

                var tokenDto = new TokenDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                    TokenType = "Bearer"
                };

                _logger.LogInformation("Login realizado com sucesso para usuário: {Email}", loginDto.Email);

                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Login realizado com sucesso.",
                    Token = tokenDto,
                    Usuario = funcionarioDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o login para usuário: {Email}", loginDto.Email);
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Erro interno do servidor."
                };
            }
        }

        public async Task<TokenDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Refresh token vazio ou nulo");
                throw new SecurityTokenException("Refresh token é obrigatório");
            }

            _logger.LogInformation("Tentativa de refresh token");

            try
            {
                var now = DateTime.UtcNow;
                var tokenHash = HashRefreshToken(refreshToken);
                var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
                if (storedToken is null || storedToken.RevokedAtUtc is not null)
                {
                    _logger.LogWarning("Refresh token inválido ou expirado");
                    throw new SecurityTokenException("Refresh token inválido");
                }

                if (now >= storedToken.ExpiresAtUtc)
                {
                    await _refreshTokenRepository.RevokeAsync(tokenHash, now);
                    _logger.LogWarning("Refresh token expirado para funcionário: {FuncionarioId}", storedToken.FuncionarioId);
                    throw new SecurityTokenException("Refresh token expirado");
                }

                // Buscar funcionário
                var funcionario = await _funcionarioRepository.GetByIdAsync(storedToken.FuncionarioId);
                if (funcionario == null || !funcionario.IsActive || !funcionario.Usuario.IsActive)
                {
                    await _refreshTokenRepository.RevokeAsync(tokenHash, now);
                    _logger.LogWarning("Funcionário não encontrado ou inativo: {FuncionarioId}", storedToken.FuncionarioId);
                    throw new SecurityTokenException("Funcionário inválido");
                }

                // Converter para DTO
                var funcionarioDto = new FuncionarioDto
                {
                    Id = funcionario.Id,
                    NomeFuncionario = funcionario.Usuario.Nome,
                    Email = funcionario.Usuario.Email ?? string.Empty,
                    Tipo = funcionario.Tipo,
                    IsActive = funcionario.IsActive
                };

                // Gerar novos tokens
                var newAccessToken = GenerateAccessToken(funcionarioDto);
                var newRefreshToken = GenerateRefreshToken();
                var newExpiresAt = DateTime.UtcNow.AddDays(7);
                var rotated = await _refreshTokenRepository.TryRotateAsync(
                    tokenHash,
                    new RefreshToken
                    {
                        TokenHash = HashRefreshToken(newRefreshToken),
                        FuncionarioId = funcionario.Id,
                        CreatedAtUtc = now,
                        ExpiresAtUtc = newExpiresAt
                    },
                    now);
                if (!rotated)
                {
                    throw new SecurityTokenException("Refresh token inválido ou já utilizado");
                }

                _logger.LogInformation(
                    "Refresh token realizado com sucesso para funcionário: {FuncionarioId}",
                    storedToken.FuncionarioId);

                return new TokenDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                    TokenType = "Bearer"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante refresh token");
                throw;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Tentativa de revogar refresh token vazio");
                return false;
            }

            _logger.LogInformation("Revogando refresh token");

            try
            {
                var revoked = await _refreshTokenRepository.RevokeAsync(
                    HashRefreshToken(refreshToken),
                    DateTime.UtcNow);
                if (revoked)
                    _logger.LogInformation("Refresh token revogado com sucesso");
                else
                    _logger.LogWarning("Tentativa de revogar refresh token inválido");
                return revoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao revogar refresh token");
                throw;
            }
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token vazio ou nulo");
                return Task.FromResult(false);
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada"));

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ValidateIssuer = true,
                    ValidIssuer = GetRequiredJwtSetting("Issuer"),
                    ValidateAudience = true,
                    ValidAudience = GetRequiredJwtSetting("Audience"),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                return Task.FromResult(validatedToken is JwtSecurityToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token inválido");
                return Task.FromResult(false);
            }
        }

        public string GenerateAccessToken(FuncionarioDto funcionario)
        {
            var secretKey = Encoding.UTF8.GetBytes(GetRequiredJwtSetting("SecretKey"));
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, funcionario.Id),
                new Claim(ClaimTypes.Name, funcionario.NomeFuncionario),
                new Claim("email", funcionario.Email),
                new Claim(ClaimTypes.Role, funcionario.Tipo.ToString()),
                new Claim("tipo_funcionario", funcionario.Tipo.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature),
                Issuer = GetRequiredJwtSetting("Issuer"),
                Audience = GetRequiredJwtSetting("Audience")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GetRequiredJwtSetting(string settingName)
        {
            var value = _configuration[$"Jwt:{settingName}"];
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"JWT {settingName} não configurado")
                : value;
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static string HashRefreshToken(string refreshToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}