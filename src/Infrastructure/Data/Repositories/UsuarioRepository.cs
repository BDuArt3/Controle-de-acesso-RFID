using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;


namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsuarioRepository> _logger;

        public UsuarioRepository(AppDbContext context, ILogger<UsuarioRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            _logger.LogInformation("Iniciando criação de usuário. Nome: {Nome}, Tipo: {Tipo}", 
                usuario.Nome, usuario.Tipo);

            try
            {
                usuario.Id = Guid.NewGuid().ToString();
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Usuário criado com sucesso. ID: {Id}, Nome: {Nome}", 
                    usuario.Id, usuario.Nome);
                
                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário. Nome: {Nome}, Tipo: {Tipo}", 
                    usuario.Nome, usuario.Tipo);
                throw;
            }
        }

        public async Task<Usuario?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando usuário por ID: {Id}", id);

            try
            {
                var usuario = await _context.Usuarios
                                     .Include(u => u.Cartoes)
                                     .Include(u => u.Funcionario)
                                     .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado para ID: {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Usuário encontrado. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                        usuario.Id, usuario.Nome, usuario.IsActive);
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Usuario?> GetByCardUidAsync(string cardUid)
        {
            _logger.LogInformation("Buscando usuário por UID do cartão: {CardUid}", cardUid);

            try
            {
                var usuario = await _context.Usuarios
                                     .Include(u => u.Cartoes)
                                     .FirstOrDefaultAsync(u => u.Cartoes.Any(c => c.Uid == cardUid));

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado para UID do cartão: {CardUid}", cardUid);
                }
                else
                {
                    _logger.LogInformation("Usuário encontrado. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}, CardUid: {CardUid}", 
                        usuario.Id, usuario.Nome, usuario.IsActive, cardUid);
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por UID do cartão: {CardUid}", cardUid);
                throw;
            }
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os usuários ativos");

            try
            {
                var usuarios = await _context.Usuarios
                                     .Include(u => u.Cartoes)
                                     .Include(u => u.Funcionario)
                                     .Where(u => u.IsActive)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} usuários ativos", usuarios.Count());

                return usuarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os usuários");
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email, string? excludingUsuarioId = null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.Usuarios.AnyAsync(usuario =>
                usuario.Email == normalizedEmail &&
                (excludingUsuarioId == null || usuario.Id != excludingUsuarioId));
        }

        public async Task<Usuario> UpdateAsync(Usuario usuario)
        {
            _logger.LogInformation("Atualizando usuário. ID: {Id}, Nome: {Nome}", 
                usuario.Id, usuario.Nome);

            try
            {
                _context.Usuarios.Update(usuario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Usuário atualizado com sucesso. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                    usuario.Id, usuario.Nome, usuario.IsActive);
                
                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar usuário. ID: {Id}, Nome: {Nome}", 
                    usuario.Id, usuario.Nome);
                throw;
            }
        }

    }
}