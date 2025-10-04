using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class AcessoService : IAcessoService
    {
        private readonly ICartaoRepository _cartaoRepository;
        private readonly ILogAcessoService _logAcessoService;
        private readonly ILogger<AcessoService> _logger;

        public AcessoService(
            ICartaoRepository cartaoRepository,
            ILogAcessoService logAcessoService,
            ILogger<AcessoService> logger)
        {
            _cartaoRepository = cartaoRepository;
            _logAcessoService = logAcessoService;
            _logger = logger;
        }

        public async Task<AccessCheckResponseDto> CheckAccessAsync(string cardUid)
        {
            _logger.LogInformation("Iniciando verificação de acesso para cartão UID: {CardUid}", cardUid);

            try
            {
                if (string.IsNullOrWhiteSpace(cardUid))
                {
                    var erroUidVazio = "UID do cartão é obrigatório.";
                    _logger.LogWarning("Tentativa de verificação de acesso com UID do cartão vazio ou nulo");
                    
                    // Registrar tentativa com erro
                    await RegistrarTentativaAcessoAsync(cardUid, false, erroUidVazio);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = erroUidVazio
                    };
                }

                // Buscar cartão no banco de dados
                var cartao = await _cartaoRepository.GetByUidAsync(cardUid);

                if (cartao == null)
                {
                    var motivoCartaoNaoEncontrado = "Cartão não encontrado no sistema.";
                    _logger.LogWarning("Acesso negado: Cartão não encontrado. UID: {CardUid}", cardUid);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, motivoCartaoNaoEncontrado);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoCartaoNaoEncontrado
                    };
                }

                // Verificar se o cartão está ativo
                if (!cartao.IsActive)
                {
                    var motivoCartaoInativo = "Cartão desativado.";
                    _logger.LogWarning("Acesso negado: Cartão inativo. UID: {CardUid}", cardUid);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, motivoCartaoInativo);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoCartaoInativo
                    };
                }

                // Verificar se o usuário existe e está ativo
                if (cartao.Usuario == null)
                {
                    var motivoUsuarioNaoEncontrado = "Usuário não encontrado para este cartão.";
                    _logger.LogWarning("Acesso negado: Usuário não encontrado para o cartão. UID: {CardUid}", cardUid);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, motivoUsuarioNaoEncontrado);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoUsuarioNaoEncontrado
                    };
                }

                if (!cartao.Usuario.IsActive)
                {
                    var motivoUsuarioInativo = "Usuário desativado.";
                    _logger.LogWarning("Acesso negado: Usuário inativo. UID: {CardUid}, Usuário: {UsuarioId}", 
                        cardUid, cartao.Usuario.Id);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, motivoUsuarioInativo);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoUsuarioInativo
                    };
                }

                // Acesso permitido - cartão e usuário válidos e ativos
                var motivoAcessoPermitido = $"Acesso autorizado para {cartao.Usuario.Nome}";
                _logger.LogInformation("Acesso permitido. UID: {CardUid}, Usuário: {UsuarioNome}, Tipo: {UsuarioTipo}", 
                    cardUid, cartao.Usuario.Nome, cartao.Usuario.Tipo);
                
                // Registrar tentativa de acesso permitido
                await RegistrarTentativaAcessoAsync(cardUid, true, motivoAcessoPermitido);
                
                return new AccessCheckResponseDto 
                { 
                    Allowed = true, 
                    Message = motivoAcessoPermitido,
                    UsuarioNome = cartao.Usuario.Nome,
                    UsuarioTipo = cartao.Usuario.Tipo.ToString()
                };
            }
            catch (Exception ex)
            {
                var motivoErroInterno = "Erro interno do sistema.";
                _logger.LogError(ex, "Erro inesperado ao verificar acesso para cartão UID: {CardUid}", cardUid);
                
                // Registrar tentativa com erro
                try
                {
                    await RegistrarTentativaAcessoAsync(cardUid, false, $"Erro interno: {ex.Message}");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Erro ao registrar log de acesso após falha");
                }
                
                return new AccessCheckResponseDto 
                { 
                    Allowed = false, 
                    Message = motivoErroInterno
                };
            }
        }

        private async Task RegistrarTentativaAcessoAsync(string cardUid, bool acessoPermitido, string motivo)
        {
            try
            {
                var createLogDto = new CreateLogAcessoDto
                {
                    CartaoUid = cardUid ?? "UID_VAZIO",
                    AcessoPermitido = acessoPermitido,
                    Motivo = motivo
                };

                await _logAcessoService.RegistrarTentativaAsync(createLogDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar tentativa de acesso no log. CartaoUid: {CardUid}", cardUid);
                // Não re-lançar a exceção para não afetar o fluxo principal
            }
        }
    }
}

