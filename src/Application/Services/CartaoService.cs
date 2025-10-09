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
        private readonly IPortadorRepository _portadorRepository;
        private readonly ILogger<CartaoService> _logger;

        public CartaoService(ICartaoRepository cartaoRepository, IPortadorRepository portadorRepository, ILogger<CartaoService> logger)
        {
            _cartaoRepository = cartaoRepository;
            _portadorRepository = portadorRepository;
            _logger = logger;
        }

        public async Task<CartaoDto> CreateAsync(CreateCartaoDto createCartaoDto)
        {
            _logger.LogInformation("Iniciando criação de cartão. UID: {Uid}, PortadorId: {PortadorId}", 
                createCartaoDto.Uid, createCartaoDto.PortadorId);

            try
            {
                if (string.IsNullOrWhiteSpace(createCartaoDto.Uid))
                {
                    throw new ArgumentException("UID do cartão é obrigatório", nameof(createCartaoDto.Uid));
                }

                if (string.IsNullOrWhiteSpace(createCartaoDto.PortadorId))
                {
                    throw new ArgumentException("ID do portador é obrigatório", nameof(createCartaoDto.PortadorId));
                }

                // Verificar se o portador existe
                var portador = await _portadorRepository.GetByIdAsync(createCartaoDto.PortadorId);
                if (portador == null)
                {
                    throw new InvalidOperationException($"Portador com ID {createCartaoDto.PortadorId} não encontrado");
                }

                // Verificar se o UID já existe
                var uidExiste = await _cartaoRepository.UidExistsAsync(createCartaoDto.Uid);
                if (uidExiste)
                {
                    throw new InvalidOperationException($"UID {createCartaoDto.Uid} já está em uso");
                }

                var cartao = new Cartao
                {
                    Uid = createCartaoDto.Uid.Trim(),
                    PortadorId = createCartaoDto.PortadorId.Trim(),
                    IsActive = true
                };

                var cartaoCriado = await _cartaoRepository.CreateAsync(cartao);

                _logger.LogInformation("Cartão criado com sucesso. ID: {Id}, UID: {Uid}", 
                    cartaoCriado.Id, cartaoCriado.Uid);

                return MapToDto(cartaoCriado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cartão. UID: {Uid}, PortadorId: {PortadorId}", 
                    createCartaoDto.Uid, createCartaoDto.PortadorId);
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

                return cartoes.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cartões");
                throw;
            }
        }

        public async Task<IEnumerable<CartaoDto>> GetByPortadorIdAsync(string portadorId)
        {
            _logger.LogInformation("Buscando cartões por PortadorId: {PortadorId}", portadorId);

            try
            {
                if (string.IsNullOrWhiteSpace(portadorId))
                {
                    throw new ArgumentException("ID do portador é obrigatório", nameof(portadorId));
                }

                var cartoes = await _cartaoRepository.GetByPortadorIdAsync(portadorId);

                _logger.LogInformation("Encontrados {Count} cartões ativos para o portador {PortadorId}", 
                    cartoes.Count(), portadorId);

                return cartoes.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartões por PortadorId: {PortadorId}", portadorId);
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

                if (string.IsNullOrWhiteSpace(updateCartaoDto.PortadorId))
                {
                    throw new ArgumentException("ID do portador é obrigatório", nameof(updateCartaoDto.PortadorId));
                }

                var cartaoExistente = await _cartaoRepository.GetByIdAsync(id);
                if (cartaoExistente == null)
                {
                    throw new InvalidOperationException($"Cartão com ID {id} não encontrado");
                }

                // Verificar se o portador existe
                var portador = await _portadorRepository.GetByIdAsync(updateCartaoDto.PortadorId);
                if (portador == null)
                {
                    throw new InvalidOperationException($"Portador com ID {updateCartaoDto.PortadorId} não encontrado");
                }

                // Verificar se o UID já existe (apenas se mudou)
                if (cartaoExistente.Uid != updateCartaoDto.Uid)
                {
                    var uidExiste = await _cartaoRepository.UidExistsAsync(updateCartaoDto.Uid);
                    if (uidExiste)
                    {
                        throw new InvalidOperationException($"UID {updateCartaoDto.Uid} já está em uso");
                    }
                }

                cartaoExistente.Uid = updateCartaoDto.Uid.Trim();
                cartaoExistente.PortadorId = updateCartaoDto.PortadorId.Trim();

                var cartaoAtualizado = await _cartaoRepository.UpdateAsync(cartaoExistente);

                _logger.LogInformation("Cartão atualizado com sucesso. ID: {Id}, UID: {Uid}", 
                    cartaoAtualizado.Id, cartaoAtualizado.Uid);

                return MapToDto(cartaoAtualizado);
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

                var resultado = await _cartaoRepository.DeleteAsync(id);

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

                var cartao = await _cartaoRepository.GetByIdAsync(id);
                if (cartao == null)
                {
                    _logger.LogWarning("Cartão não encontrado para ativação. ID: {Id}", id);
                    return false;
                }

                cartao.IsActive = true;
                await _cartaoRepository.UpdateAsync(cartao);

                _logger.LogInformation("Cartão ativado com sucesso. ID: {Id}, UID: {Uid}", 
                    cartao.Id, cartao.Uid);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar cartão. ID: {Id}", id);
                throw;
            }
        }

        private static CartaoDto MapToDto(Cartao cartao)
        {
            return new CartaoDto
            {
                Id = cartao.Id,
                Uid = cartao.Uid,
                IsActive = cartao.IsActive,
                PortadorId = cartao.PortadorId,
                NomePortador = cartao.Portador?.Nome ?? "Portador não encontrado"
            };
        }
    }
}