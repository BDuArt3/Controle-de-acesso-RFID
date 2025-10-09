using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class PortadorService : IPortadorService
    {
        private readonly IPortadorRepository _portadorRepository;
        private readonly ILogger<PortadorService> _logger;

        public PortadorService(IPortadorRepository portadorRepository, ILogger<PortadorService> logger)
        {
            _portadorRepository = portadorRepository;
            _logger = logger;
        }

        public async Task<PortadorDto> CreateAsync(CreatePortadorDto createPortadorDto)
        {
            _logger.LogInformation("Iniciando criação de portador. Nome: {Nome}, Tipo: {Tipo}", 
                createPortadorDto.Nome, createPortadorDto.Tipo);

            try
            {
                if (string.IsNullOrWhiteSpace(createPortadorDto.Nome))
                {
                    throw new ArgumentException("Nome do portador é obrigatório", nameof(createPortadorDto.Nome));
                }

                var portador = new Portador
                {
                    Nome = createPortadorDto.Nome.Trim(),
                    Tipo = createPortadorDto.Tipo,
                    IsActive = true
                };

                var portadorCriado = await _portadorRepository.CreateAsync(portador);

                _logger.LogInformation("Portador criado com sucesso. ID: {Id}, Nome: {Nome}", 
                    portadorCriado.Id, portadorCriado.Nome);

                return MapToDto(portadorCriado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar portador. Nome: {Nome}, Tipo: {Tipo}", 
                    createPortadorDto.Nome, createPortadorDto.Tipo);
                throw;
            }
        }

        public async Task<PortadorDto?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando portador por ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var portador = await _portadorRepository.GetByIdAsync(id);

                if (portador == null)
                {
                    _logger.LogWarning("Portador não encontrado para ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Portador encontrado. ID: {Id}, Nome: {Nome}", portador.Id, portador.Nome);

                return MapToDto(portador);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar portador por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PortadorDto>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os portadors ativos");

            try
            {
                var portadors = await _portadorRepository.GetAllAsync();

                _logger.LogInformation("Encontrados {Count} portadors ativos", portadors.Count());

                return portadors.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os portadors");
                throw;
            }
        }

        public async Task<PortadorDto> UpdateAsync(string id, CreatePortadorDto updatePortadorDto)
        {
            _logger.LogInformation("Atualizando portador. ID: {Id}, Nome: {Nome}", id, updatePortadorDto.Nome);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                if (string.IsNullOrWhiteSpace(updatePortadorDto.Nome))
                {
                    throw new ArgumentException("Nome do portador é obrigatório", nameof(updatePortadorDto.Nome));
                }

                var portadorExistente = await _portadorRepository.GetByIdAsync(id);
                if (portadorExistente == null)
                {
                    throw new InvalidOperationException($"Portador com ID {id} não encontrado");
                }

                portadorExistente.Nome = updatePortadorDto.Nome.Trim();
                portadorExistente.Tipo = updatePortadorDto.Tipo;

                var portadorAtualizado = await _portadorRepository.UpdateAsync(portadorExistente);

                _logger.LogInformation("Portador atualizado com sucesso. ID: {Id}, Nome: {Nome}", 
                    portadorAtualizado.Id, portadorAtualizado.Nome);

                return MapToDto(portadorAtualizado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar portador. ID: {Id}, Nome: {Nome}", id, updatePortadorDto.Nome);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de portador. ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var resultado = await _portadorRepository.DeleteAsync(id);

                if (resultado)
                {
                    _logger.LogInformation("Portador desativado com sucesso. ID: {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Portador não encontrado para desativação. ID: {Id}", id);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar portador. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ActivateAsync(string id)
        {
            _logger.LogInformation("Iniciando ativação de portador. ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var portador = await _portadorRepository.GetByIdAsync(id);
                if (portador == null)
                {
                    _logger.LogWarning("Portador não encontrado para ativação. ID: {Id}", id);
                    return false;
                }

                portador.IsActive = true;
                await _portadorRepository.UpdateAsync(portador);

                _logger.LogInformation("Portador ativado com sucesso. ID: {Id}, Nome: {Nome}", 
                    portador.Id, portador.Nome);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar portador. ID: {Id}", id);
                throw;
            }
        }

        private static PortadorDto MapToDto(Portador portador)
        {
            return new PortadorDto
            {
                Id = portador.Id,
                Nome = portador.Nome,
                Tipo = portador.Tipo,
                IsActive = portador.IsActive,
                Cartoes = portador.Cartoes?.Select(c => new CartaoDto
                {
                    Id = c.Id,
                    Uid = c.Uid,
                    IsActive = c.IsActive,
                    PortadorId = c.PortadorId
                }).ToList() ?? new List<CartaoDto>()
            };
        }
    }
}