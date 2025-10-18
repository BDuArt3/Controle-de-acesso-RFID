using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class CartaoRepository : ICartaoRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartaoRepository> _logger;

        public CartaoRepository(AppDbContext context, ILogger<CartaoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Cartao> CreateAsync(Cartao cartao)
        {
            _logger.LogInformation("Iniciando criação de cartão. UID: {Uid}, UsuarioId: {UsuarioId}", 
                cartao.Uid, cartao.UsuarioId);

            try
            {
                cartao.Id = Guid.NewGuid().ToString();
                _context.Cartoes.Add(cartao);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cartão criado com sucesso. ID: {Id}, UID: {Uid}", 
                    cartao.Id, cartao.Uid);
                
                return cartao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cartão. UID: {Uid}, UsuarioId: {UsuarioId}", 
                    cartao.Uid, cartao.UsuarioId);
                throw;
            }
        }

        public async Task<Cartao?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando cartão por ID: {Id}", id);

            try
            {
                var cartao = await _context.Cartoes
                                    .Include(c => c.Usuario)
                                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cartao == null)
                {
                    _logger.LogWarning("Cartão não encontrado para ID: {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Cartão encontrado. ID: {Id}, UID: {Uid}, Ativo: {IsActive}", 
                        cartao.Id, cartao.Uid, cartao.IsActive);
                }

                return cartao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Cartao?> GetByUidAsync(string uid)
        {
            _logger.LogInformation("Buscando cartão por UID: {Uid}", uid);

            try
            {
                var cartao = await _context.Cartoes
                                    .Include(c => c.Usuario)
                                    .FirstOrDefaultAsync(c => c.Uid == uid);

                if (cartao == null)
                {
                    _logger.LogWarning("Cartão não encontrado para UID: {Uid}", uid);
                }
                else
                {
                    _logger.LogInformation("Cartão encontrado. ID: {Id}, UID: {Uid}, Ativo: {IsActive}", 
                        cartao.Id, cartao.Uid, cartao.IsActive);
                }

                return cartao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartão por UID: {Uid}", uid);
                throw;
            }
        }

        public async Task<IEnumerable<Cartao>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os cartões (incluindo inativos)");

            try
            {
                var cartoes = await _context.Cartoes
                                     .Include(c => c.Usuario)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} cartões", cartoes.Count());

                return cartoes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cartões");
                throw;
            }
        }

        public async Task<IEnumerable<Cartao>> GetByUsuarioIdAsync(string usuarioId)
        {
            _logger.LogInformation("Buscando cartões por UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                var cartoes = await _context.Cartoes
                                     .Include(c => c.Usuario)
                                     .Where(c => c.UsuarioId == usuarioId && c.IsActive)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} cartões ativos para o usuário {UsuarioId}", 
                    cartoes.Count(), usuarioId);

                return cartoes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartões por UsuarioId: {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<IEnumerable<Cartao>> GetAllByUsuarioIdAsync(string usuarioId)
        {
            _logger.LogInformation("Buscando todos os cartões (ativos e inativos) por UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                var cartoes = await _context.Cartoes
                                     .Include(c => c.Usuario)
                                     .Where(c => c.UsuarioId == usuarioId)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} cartões (ativos e inativos) para o usuário {UsuarioId}", 
                    cartoes.Count(), usuarioId);

                return cartoes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cartões por UsuarioId: {UsuarioId}", usuarioId);
                throw;
            }
        }

        public async Task<Cartao> UpdateAsync(Cartao cartao)
        {
            _logger.LogInformation("Atualizando cartão. ID: {Id}, UID: {Uid}", 
                cartao.Id, cartao.Uid);

            try
            {
                _context.Cartoes.Update(cartao);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cartão atualizado com sucesso. ID: {Id}, UID: {Uid}, Ativo: {IsActive}", 
                    cartao.Id, cartao.Uid, cartao.IsActive);
                
                return cartao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cartão. ID: {Id}, UID: {Uid}", 
                    cartao.Id, cartao.Uid);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de cartão. ID: {Id}", id);

            try
            {
                var cartao = await GetByIdAsync(id);
                if (cartao == null)
                {
                    _logger.LogWarning("Tentativa de desativar cartão inexistente. ID: {Id}", id);
                    return false;
                }

                cartao.IsActive = false;
                await UpdateAsync(cartao);
                
                _logger.LogInformation("Cartão desativado com sucesso. ID: {Id}, UID: {Uid}", 
                    cartao.Id, cartao.Uid);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar cartão. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> UidExistsAsync(string uid)
        {
            _logger.LogInformation("Verificando se UID de cartão já existe: {Uid}", uid);

            try
            {
                var exists = await _context.Cartoes
                                   .AnyAsync(c => c.Uid == uid);

                _logger.LogInformation("Verificação de UID de cartão concluída. UID: {Uid}, Existe: {Exists}", 
                    uid, exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se UID de cartão existe: {Uid}", uid);
                throw;
            }
        }
    }
}