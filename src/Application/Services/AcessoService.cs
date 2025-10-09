using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
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
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.UidObrigatorio, erroUidVazio);
                    
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
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.CartaoNaoEncontrado, motivoCartaoNaoEncontrado);
                    
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
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.CartaoDesativado, motivoCartaoInativo);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoCartaoInativo
                    };
                }

                // Verificar se o portador existe e está ativo
                if (cartao.Portador == null)
                {
                    var motivoPortadorNaoEncontrado = "Portador não encontrado para este cartão.";
                    _logger.LogWarning("Acesso negado: Portador não encontrado para o cartão. UID: {CardUid}", cardUid);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.PortadorNaoEncontrado, motivoPortadorNaoEncontrado);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoPortadorNaoEncontrado
                    };
                }

                if (!cartao.Portador.IsActive)
                {
                    var motivoPortadorInativo = "Portador desativado.";
                    _logger.LogWarning("Acesso negado: Portador inativo. UID: {CardUid}, Portador: {PortadorId}", 
                        cardUid, cartao.Portador.Id);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.PortadorDesativado, motivoPortadorInativo);
                    
                    return new AccessCheckResponseDto 
                    { 
                        Allowed = false, 
                        Message = motivoPortadorInativo
                    };
                }

                // Acesso permitido - cartão e portador válidos e ativos
                var motivoAcessoPermitido = $"Acesso autorizado para {cartao.Portador.Nome}";
                _logger.LogInformation("Acesso permitido. UID: {CardUid}, Portador: {PortadorNome}, Tipo: {PortadorTipo}", 
                    cardUid, cartao.Portador.Nome, cartao.Portador.Tipo);
                
                // Registrar tentativa de acesso permitido
                await RegistrarTentativaAcessoAsync(cardUid, true, TipoMotivoAcesso.AcessoAutorizado, motivoAcessoPermitido);
                
                return new AccessCheckResponseDto 
                { 
                    Allowed = true, 
                    Message = motivoAcessoPermitido,
                    PortadorNome = cartao.Portador.Nome,
                    PortadorTipo = cartao.Portador.Tipo.ToString()
                };
            }
            catch (Exception ex)
            {
                var motivoErroInterno = "Erro interno do sistema.";
                _logger.LogError(ex, "Erro inesperado ao verificar acesso para cartão UID: {CardUid}", cardUid);
                
                // Registrar tentativa com erro
                try
                {
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.ErroInterno, $"Erro interno: {ex.Message}");
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

        private async Task RegistrarTentativaAcessoAsync(string cardUid, bool acessoPermitido, TipoMotivoAcesso motivoTipo, string motivo)
        {
            try
            {
                var createLogDto = new CreateLogAcessoDto
                {
                    CartaoUid = cardUid ?? "UID_VAZIO",
                    AcessoPermitido = acessoPermitido,
                    MotivoTipo = motivoTipo,
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

