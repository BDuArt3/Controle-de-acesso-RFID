using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;


namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class PortadorRepository : IPortadorRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PortadorRepository> _logger;

        public PortadorRepository(AppDbContext context, ILogger<PortadorRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Portador> CreateAsync(Portador portador)
        {
            _logger.LogInformation("Iniciando criação de portador. Nome: {Nome}, Tipo: {Tipo}", 
                portador.Nome, portador.Tipo);

            try
            {
                portador.Id = Guid.NewGuid().ToString();
                _context.Portadors.Add(portador);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Portador criado com sucesso. ID: {Id}, Nome: {Nome}", 
                    portador.Id, portador.Nome);
                
                return portador;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar portador. Nome: {Nome}, Tipo: {Tipo}", 
                    portador.Nome, portador.Tipo);
                throw;
            }
        }

        public async Task<Portador?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando portador por ID: {Id}", id);

            try
            {
                var portador = await _context.Portadors
                                     .Include(u => u.Cartoes)
                                     .FirstOrDefaultAsync(u => u.Id == id);

                if (portador == null)
                {
                    _logger.LogWarning("Portador não encontrado para ID: {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Portador encontrado. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                        portador.Id, portador.Nome, portador.IsActive);
                }

                return portador;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar portador por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Portador?> GetByCardUidAsync(string cardUid)
        {
            _logger.LogInformation("Buscando portador por UID do cartão: {CardUid}", cardUid);

            try
            {
                var portador = await _context.Portadors
                                     .Include(u => u.Cartoes)
                                     .FirstOrDefaultAsync(u => u.Cartoes.Any(c => c.Uid == cardUid));

                if (portador == null)
                {
                    _logger.LogWarning("Portador não encontrado para UID do cartão: {CardUid}", cardUid);
                }
                else
                {
                    _logger.LogInformation("Portador encontrado. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}, CardUid: {CardUid}", 
                        portador.Id, portador.Nome, portador.IsActive, cardUid);
                }

                return portador;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar portador por UID do cartão: {CardUid}", cardUid);
                throw;
            }
        }

        public async Task<IEnumerable<Portador>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os portadors ativos");

            try
            {
                var portadors = await _context.Portadors
                                     .Include(u => u.Cartoes)
                                     .Where(u => u.IsActive)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} portadors ativos", portadors.Count());

                return portadors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os portadors");
                throw;
            }
        }

        public async Task<Portador> UpdateAsync(Portador portador)
        {
            _logger.LogInformation("Atualizando portador. ID: {Id}, Nome: {Nome}", 
                portador.Id, portador.Nome);

            try
            {
                _context.Portadors.Update(portador);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Portador atualizado com sucesso. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                    portador.Id, portador.Nome, portador.IsActive);
                
                return portador;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar portador. ID: {Id}, Nome: {Nome}", 
                    portador.Id, portador.Nome);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de portador. ID: {Id}", id);

            try
            {
                var portador = await GetByIdAsync(id);
                if (portador == null)
                {
                    _logger.LogWarning("Tentativa de desativar portador inexistente. ID: {Id}", id);
                    return false;
                }

                portador.IsActive = false;
                await UpdateAsync(portador);
                
                _logger.LogInformation("Portador desativado com sucesso. ID: {Id}, Nome: {Nome}", 
                    portador.Id, portador.Nome);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar portador. ID: {Id}", id);
                throw;
            }
        }
    }
}