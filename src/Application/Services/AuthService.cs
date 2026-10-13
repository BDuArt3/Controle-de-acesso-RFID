using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly Dictionary<string, (string FuncionarioId, DateTime ExpiresAt)> _refreshTokens;

        public AuthService(
            IFuncionarioRepository funcionarioRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _funcionarioRepository = funcionarioRepository;
            _configuration = configuration;
            _logger = logger;
            _refreshTokens = new Dictionary<string, (string, DateTime)>();
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("Tentativa de login para email: {Email}", loginDto.Email);

            try
            {
                // Buscar funcionário por email
                var funcionario = await _funcionarioRepository.GetByEmailAsync(loginDto.Email);

                if (funcionario == null)
                {
                    _logger.LogWarning("Email não encontrado: {Email}", loginDto.Email);
                    return new LoginResponseDto
                    {
                        Success = false,
                        Message = "Email ou senha inválidos."
                    };
                }

                // Verificar se o funcionário está ativo
                if (!funcionario.IsActive)
                {
                    _logger.LogWarning("Tentativa de login com usuário inativo: {email}", loginDto.Email);
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
                    NomeFuncionario = funcionario.NomeFuncionario,
                    Email = funcionario.Email,
                    Tipo = funcionario.Tipo,
                    IsActive = funcionario.IsActive
                };

                // Gerar tokens
                var accessToken = GenerateAccessToken(funcionarioDto);
                var refreshToken = GenerateRefreshToken();

                // Armazenar refresh token
                var expiresAt = DateTime.UtcNow.AddDays(7); // Refresh token válido por 7 dias
                _refreshTokens[refreshToken] = (funcionario.Id, expiresAt);

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
            _logger.LogInformation("Tentativa de refresh token");

            try
            {
                if (!_refreshTokens.ContainsKey(refreshToken))
                {
                    _logger.LogWarning("Refresh token inválido ou expirado");
                    throw new SecurityTokenException("Refresh token inválido");
                }

                var (funcionarioId, expiresAt) = _refreshTokens[refreshToken];

                if (DateTime.UtcNow > expiresAt)
                {
                    _refreshTokens.Remove(refreshToken);
                    _logger.LogWarning("Refresh token expirado para funcionário: {FuncionarioId}", funcionarioId);
                    throw new SecurityTokenException("Refresh token expirado");
                }

                // Buscar funcionário
                var funcionario = await _funcionarioRepository.GetByIdAsync(funcionarioId);
                if (funcionario == null || !funcionario.IsActive)
                {
                    _refreshTokens.Remove(refreshToken);
                    _logger.LogWarning("Funcionário não encontrado ou inativo: {FuncionarioId}", funcionarioId);
                    throw new SecurityTokenException("Funcionário inválido");
                }

                // Converter para DTO
                var funcionarioDto = new FuncionarioDto
                {
                    Id = funcionario.Id,
                    NomeFuncionario = funcionario.NomeFuncionario,
                    Email = funcionario.Email,
                    Tipo = funcionario.Tipo,
                    IsActive = funcionario.IsActive
                };

                // Gerar novos tokens
                var newAccessToken = GenerateAccessToken(funcionarioDto);
                var newRefreshToken = GenerateRefreshToken();

                // Remover o refresh token antigo e adicionar o novo
                _refreshTokens.Remove(refreshToken);
                var newExpiresAt = DateTime.UtcNow.AddDays(7);
                _refreshTokens[newRefreshToken] = (funcionario.Id, newExpiresAt);

                _logger.LogInformation("Refresh token realizado com sucesso para funcionário: {FuncionarioId}", funcionarioId);

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

        public Task<bool> RevokeTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Revogando refresh token");

            try
            {
                if (_refreshTokens.ContainsKey(refreshToken))
                {
                    _refreshTokens.Remove(refreshToken);
                    _logger.LogInformation("Refresh token revogado com sucesso");
                    return Task.FromResult(true);
                }

                _logger.LogWarning("Tentativa de revogar refresh token inválido");
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao revogar refresh token");
                return Task.FromResult(false);
            }
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
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
    }
}