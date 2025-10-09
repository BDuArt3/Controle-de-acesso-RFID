using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ControleDeAcesso.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Realiza o login de um funcionário
        /// </summary>
        /// <param name="loginDto">Dados de login</param>
        /// <returns>Token de acesso e informações do usuário</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Tentativa de login para usuário: {UsuarioAcesso}", loginDto.UsuarioAcesso);

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var resultado = await _authService.LoginAsync(loginDto);

                if (resultado.Success)
                {
                    _logger.LogInformation("Login realizado com sucesso para usuário: {UsuarioAcesso}", loginDto.UsuarioAcesso);
                    return Ok(resultado);
                }
                else
                {
                    _logger.LogWarning("Falha no login para usuário: {UsuarioAcesso} - {Message}", 
                        loginDto.UsuarioAcesso, resultado.Message);
                    return Unauthorized(resultado);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante login para usuário: {UsuarioAcesso}", loginDto.UsuarioAcesso);
                return StatusCode(500, new LoginResponseDto 
                { 
                    Success = false, 
                    Message = "Erro interno do servidor" 
                });
            }
        }

        /// <summary>
        /// Renova o token de acesso usando o refresh token
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token</param>
        /// <returns>Novo token de acesso</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            _logger.LogInformation("Tentativa de refresh token");

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var novoToken = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
                
                _logger.LogInformation("Refresh token realizado com sucesso");
                return Ok(novoToken);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Refresh token inválido: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante refresh token");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Revoga um refresh token (logout)
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token a ser revogado</param>
        /// <returns>Status da operação</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto refreshTokenDto)
        {
            _logger.LogInformation("Tentativa de logout");

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sucesso = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);
                
                if (sucesso)
                {
                    _logger.LogInformation("Logout realizado com sucesso");
                    return Ok(new { message = "Logout realizado com sucesso" });
                }
                else
                {
                    _logger.LogWarning("Falha no logout - refresh token inválido");
                    return BadRequest(new { message = "Refresh token inválido" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante logout");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Verifica se o usuário está autenticado (endpoint de teste)
        /// </summary>
        /// <returns>Informações do usuário autenticado</returns>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var usuarioAcesso = User.FindFirst("usuario_acesso")?.Value;

                return Ok(new
                {
                    id = userId,
                    nome = userName,
                    usuarioAcesso = usuarioAcesso,
                    tipo = userRole,
                    isAuthenticated = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações do usuário autenticado");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}