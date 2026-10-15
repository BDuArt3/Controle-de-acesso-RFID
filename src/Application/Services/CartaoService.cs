using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class CartaoService : ICartaoService
    {
        private readonly ICartaoRepository _cartaoRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartaoService> _logger;

        public CartaoService(
            ICartaoRepository cartaoRepository,
            IUsuarioRepository usuarioRepository,
            IUnitOfWork unitOfWork,
            ILogger<CartaoService> logger)
        {
            _cartaoRepository = cartaoRepository;
            _usuarioRepository = usuarioRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartaoDto> CreateAsync(CreateCartaoDto createCartaoDto)
        {
            _logger.LogInformation("Iniciando criação de cartão. UID: {Uid}, UsuarioId: {UsuarioId}", 
                createCartaoDto.Uid, createCartaoDto.UsuarioId);

            try
            {
                if (string.IsNullOrWhiteSpace(createCartaoDto.Uid))
                {
                    throw new ArgumentException("UID do cartão é obrigatório", nameof(createCartaoDto.Uid));
                }

                if (string.IsNullOrWhiteSpace(createCartaoDto.UsuarioId))
                {
                    throw new ArgumentException("ID do usuário é obrigatório", nameof(createCartaoDto.UsuarioId));
                }

                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var usuarioId = createCartaoDto.UsuarioId.Trim();
                    var uid = createCartaoDto.Uid.Trim();
                    var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
                    if (usuario == null)
                    {
                        throw new InvalidOperationException($"Usuário com ID {usuarioId} não encontrado");
                    }

                    if (!usuario.IsActive)
                    {
                        throw new InvalidOperationException($"Usuário com ID {usuarioId} está inativo");
                    }

                    var cartaoExistente = await _cartaoRepository.GetByUidAsync(uid);
                    if (cartaoExistente != null)
                    {
                        if (cartaoExistente.UsuarioId == usuarioId)
                        {
                            return MapToDto(cartaoExistente);
                        }

                        throw new InvalidOperationException($"UID {uid} já está em uso");
                    }

                    var agora = DateTime.UtcNow;
                    var cartao = new Cartao
                    {
                        Uid = uid,
                        UsuarioId = usuarioId,
                        DataRegistro = agora
                    };
                    var historicoAssociacao = new HistoricoAssociacoesCartao
                    {
                        CartaoId = cartao.Id,
                        DataAssociacao = agora
                    };
                    var historicoStatus = new HistoricoStatusCartao
                    {
                        CartaoId = cartao.Id,
                        Status = StatusCartao.Ativo,
                        DataHora = agora,
                        Motivo = "Emissão inicial do cartão."
                    };

                    var cartaoCriado = await _cartaoRepository.CreateWithAuditAsync(
                        cartao,
                        historicoAssociacao,
                        historicoStatus);

                    _logger.LogInformation(
                        "Cartão criado com seus históricos iniciais. ID: {Id}, UsuarioId: {UsuarioId}",
                        cartaoCriado.Id,
                        cartaoCriado.UsuarioId);

                    return MapToDto(cartaoCriado, usuario.Nome);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cartão. UID: {Uid}, UsuarioId: {UsuarioId}", 
                    createCartaoDto.Uid, createCartaoDto.UsuarioId);
                throw;
            }
        }

        public async Task<CartaoDto?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando cartão por ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var cartao = await _cartaoRepository.GetByIdAsync(id);

                if (cartao == null)
                {
                    _logger.LogWarning("Cartão não encontrado para ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Cartão encontrado. ID: {Id}, UID: {Uid}", cartao.Id, cartao.Uid);

                return MapToDto(cartao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por ID: {Id}", id);
                throw;
            }
        }

        public async Task<CartaoDto?> GetByUidAsync(string uid)
        {
            _logger.LogInformation("Buscando cartão por UID: {Uid}", uid);

            try
            {
                if (string.IsNullOrWhiteSpace(uid))
                {
                    throw new ArgumentException("UID é obrigatório", nameof(uid));
                }

                var cartao = await _cartaoRepository.GetByUidAsync(uid);

                if (cartao == null)
                {
                    _logger.LogWarning("Cartão não encontrado para UID: {Uid}", uid);
                    return null;
                }

                _logger.LogInformation("Cartão encontrado. ID: {Id}, UID: {Uid}", cartao.Id, cartao.Uid);

                return MapToDto(cartao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por UID: {Uid}", uid);
                throw;
            }
        }

        public async Task<IEnumerable<CartaoDto>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os cartões (incluindo inativos)");

            try
            {
                var cartoes = await _cartaoRepository.GetAllAsync();

                _logger.LogInformation("Encontrados {Count} cartões", cartoes.Count());

                return cartoes.Select(cartao => MapToDto(cartao));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cartões");
                throw;
            }
        }

        public async Task<IEnumerable<CartaoDto>> GetByUsuarioIdAsync(string usuarioId)
        {
            _logger.LogInformation("Buscando cartões por UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    throw new ArgumentException("ID do usuário é obrigatório", nameof(usuarioId));
                }

                var cartoes = await _cartaoRepository.GetByUsuarioIdAsync(usuarioId);

                _logger.LogInformation("Encontrados {Count} cartões ativos para o usuário {UsuarioId}", 
                    cartoes.Count(), usuarioId);

                return cartoes.Select(cartao => MapToDto(cartao));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartões por UsuarioId: {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<CartaoDto> UpdateAsync(string id, CreateCartaoDto updateCartaoDto)
        {
            _logger.LogInformation("Atualizando cartão. ID: {Id}, UID: {Uid}", id, updateCartaoDto.Uid);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                if (string.IsNullOrWhiteSpace(updateCartaoDto.Uid))
                {
                    throw new ArgumentException("UID do cartão é obrigatório", nameof(updateCartaoDto.Uid));
                }

                if (string.IsNullOrWhiteSpace(updateCartaoDto.UsuarioId))
                {
                    throw new ArgumentException("ID do usuário é obrigatório", nameof(updateCartaoDto.UsuarioId));
                }

                var cartaoExistente = await _cartaoRepository.GetByIdAsync(id);
                if (cartaoExistente == null)
                {
                    throw new KeyNotFoundException($"Cartão com ID {id} não encontrado");
                }

                if (cartaoExistente.Uid != updateCartaoDto.Uid.Trim() ||
                    cartaoExistente.UsuarioId != updateCartaoDto.UsuarioId.Trim())
                {
                    throw new InvalidOperationException(
                        "UID e titular são imutáveis nesta emissão. Para trocar o titular, cancele o cartão e crie uma nova emissão.");
                }

                return MapToDto(cartaoExistente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cartão. ID: {Id}, UID: {Uid}", id, updateCartaoDto.Uid);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de cartão. ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var resultado = await ChangeStatusAsync(
                    id,
                    StatusCartao.Bloqueado,
                    "Cartão bloqueado pela operação de exclusão lógica.");

                if (resultado)
                {
                    _logger.LogInformation("Cartão desativado com sucesso. ID: {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Cartão não encontrado para desativação. ID: {Id}", id);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar cartão. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ActivateAsync(string id)
        {
            _logger.LogInformation("Iniciando ativação de cartão. ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                return await ChangeStatusAsync(
                    id,
                    StatusCartao.Ativo,
                    "Cartão ativado explicitamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar cartão. ID: {Id}", id);
                throw;
            }
        }

        private async Task<bool> ChangeStatusAsync(string id, StatusCartao novoStatus, string motivo)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var cartao = await _cartaoRepository.GetByIdAsync(id);
                if (cartao == null)
                {
                    return false;
                }

                if (!cartao.AlterarStatus(novoStatus))
                {
                    return true;
                }

                await _cartaoRepository.UpdateStatusWithHistoryAsync(
                    cartao,
                    new HistoricoStatusCartao
                    {
                        CartaoId = cartao.Id,
                        Status = novoStatus,
                        DataHora = DateTime.UtcNow,
                        Motivo = motivo
                    });

                return true;
            });
        }

        private static CartaoDto MapToDto(Cartao cartao, string? nomeUsuario = null)
        {
            return new CartaoDto
            {
                Id = cartao.Id,
                Uid = cartao.Uid,
                IsActive = cartao.Status == StatusCartao.Ativo,
                UsuarioId = cartao.UsuarioId,
                NomeUsuario = nomeUsuario ?? cartao.Usuario?.Nome ?? "Usuário não encontrado"
            };
        }
    }
}