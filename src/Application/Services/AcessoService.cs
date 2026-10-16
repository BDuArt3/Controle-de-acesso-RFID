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
            Cartao? cartao = null;

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

                cardUid = cardUid.Trim();
                // Buscar cartão no banco de dados
                cartao = await _cartaoRepository.GetByUidAsync(cardUid);

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
                if (cartao.Status != StatusCartao.Ativo)
                {
                    var motivoCartaoInativo = "Cartão desativado.";
                    _logger.LogWarning("Acesso negado: Cartão inativo. UID: {CardUid}", cardUid);
                    
                    // Registrar tentativa de acesso negado
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.CartaoDesativado, motivoCartaoInativo, cartao);
                    
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
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.UsuarioNaoEncontrado, motivoUsuarioNaoEncontrado, cartao);
                    
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
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.UsuarioDesativado, motivoUsuarioInativo, cartao);
                    
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
                await RegistrarTentativaAcessoAsync(cardUid, true, TipoMotivoAcesso.AcessoAutorizado, motivoAcessoPermitido, cartao);
                
                return new AccessCheckResponseDto 
                { 
                    Allowed = true, 
                    Message = motivoAcessoPermitido,
                    UsuarioId = cartao.Usuario.Id,
                    NomeUsuario = cartao.Usuario.Nome,
                    UsuarioNome = cartao.Usuario.Nome,
                    UsuarioTipo = cartao.Usuario.Tipo.ToString()
                };
            }
            catch (FalhaAuditoriaException ex)
            {
                _logger.LogCritical(
                    ex.InnerException,
                    "Acesso negado porque não foi possível persistir a auditoria. CartaoId: {CartaoId}",
                    cartao?.Id);

                return new AccessCheckResponseDto
                {
                    Allowed = false,
                    Message = "Acesso negado: não foi possível registrar a auditoria."
                };
            }
            catch (Exception ex)
            {
                var motivoErroInterno = "Erro interno do sistema.";
                _logger.LogError(ex, "Erro inesperado ao verificar acesso para cartão UID: {CardUid}", cardUid);
                
                // Registrar tentativa com erro
                try
                {
                    await RegistrarTentativaAcessoAsync(cardUid, false, TipoMotivoAcesso.ErroInterno, "Erro interno ao verificar o acesso.", cartao);
                }
                catch (FalhaAuditoriaException logEx)
                {
                    _logger.LogCritical(
                        logEx.InnerException,
                        "Acesso negado e auditoria indisponível após erro de verificação. CartaoId: {CartaoId}",
                        cartao?.Id);

                    motivoErroInterno = "Acesso negado: não foi possível registrar a auditoria.";
                }
                
                return new AccessCheckResponseDto 
                { 
                    Allowed = false, 
                    Message = motivoErroInterno
                };
            }
        }

        private async Task RegistrarTentativaAcessoAsync(
            string? cardUid,
            bool acessoPermitido,
            TipoMotivoAcesso motivoTipo,
            string motivo,
            Cartao? cartao = null)
        {
            var createLogDto = new CreateLogAcessoDto
            {
                CartaoId = cartao?.Id,
                UsuarioId = cartao?.UsuarioId,
                UidInformado = cartao is null && !string.IsNullOrWhiteSpace(cardUid)
                    ? cardUid.Trim()
                    : null,
                AcessoPermitido = acessoPermitido,
                MotivoTipo = motivoTipo,
                Motivo = motivo
            };

            try
            {
                await _logAcessoService.RegistrarTentativaAsync(createLogDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar tentativa de acesso no log. CartaoUid: {CardUid}", cardUid);
                throw new FalhaAuditoriaException(ex);
            }
        }

        private sealed class FalhaAuditoriaException : Exception
        {
            public FalhaAuditoriaException(Exception innerException)
                : base("Não foi possível persistir o log de acesso.", innerException)
            {
            }
        }
    }
}
