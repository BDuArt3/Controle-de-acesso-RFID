using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class FuncionarioRepository : IFuncionarioRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FuncionarioRepository> _logger;

        public FuncionarioRepository(AppDbContext context, ILogger<FuncionarioRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Funcionario> CreateAsync(Funcionario funcionario)
        {
            _logger.LogInformation("Iniciando criação de funcionário vinculado ao usuário {UsuarioId}", funcionario.UsuarioId);

            try
            {
                funcionario.Id = Guid.NewGuid().ToString();
                _context.Funcionarios.Add(funcionario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Funcionário criado com sucesso. ID: {Id}, UsuarioId: {UsuarioId}",
                    funcionario.Id, funcionario.UsuarioId);
                
                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar funcionário. UsuarioId: {UsuarioId}", funcionario.UsuarioId);
                throw;
            }
        }

        public async Task<Funcionario?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando funcionário por ID: {Id}", id);

            try
            {
                var funcionario = await _context.Funcionarios
                                     .Include(f => f.Usuario)
                                     .FirstOrDefaultAsync(f => f.Id == id);

                if (funcionario == null)
                {
                    _logger.LogWarning("Funcionário não encontrado para ID: {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Funcionário encontrado. ID: {Id}, Ativo: {IsActive}",
                        funcionario.Id, funcionario.IsActive);
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar funcionário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Funcionario?> GetByEmailAsync(string Email)
        {
            var normalizedEmail = NormalizeEmail(Email);
            _logger.LogInformation("Buscando funcionário por email");

            try
            {
                var funcionario = await _context.Funcionarios
                                     .Include(f => f.Usuario)
                                     .FirstOrDefaultAsync(f => f.Usuario.Email == normalizedEmail);

                if (funcionario == null)
                {
                    _logger.LogWarning("Funcionário não encontrado para o email informado");
                }
                else
                {
                    _logger.LogInformation("Funcionário encontrado por email. ID: {Id}, Ativo: {IsActive}",
                        funcionario.Id, funcionario.IsActive);
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar funcionário por email");
                throw;
            }
        }

        public async Task<IEnumerable<Funcionario>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os funcionários ativos");

            try
            {
                var funcionarios = await _context.Funcionarios
                                     .Include(f => f.Usuario)
                                     .Where(f => f.IsActive)
                                     .ToListAsync();

                _logger.LogInformation("Encontrados {Count} funcionários ativos", funcionarios.Count());

                return funcionarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os funcionários");
                throw;
            }
        }

        public async Task<Funcionario> UpdateAsync(Funcionario funcionario)
        {
            _logger.LogInformation("Atualizando funcionário. ID: {Id}", funcionario.Id);

            try
            {
                _context.Funcionarios.Update(funcionario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Funcionário atualizado com sucesso. ID: {Id}, Ativo: {IsActive}",
                    funcionario.Id, funcionario.IsActive);
                
                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar funcionário. ID: {Id}", funcionario.Id);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string Email)
        {
            var normalizedEmail = NormalizeEmail(Email);
            _logger.LogInformation("Verificando se o email de acesso já existe");

            try
            {
                var exists = await _context.Funcionarios
                                   .AnyAsync(f => f.Usuario.Email == normalizedEmail);

                _logger.LogInformation("Verificação de email concluída. Existe: {Exists}", exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se o email de acesso existe");
                throw;
            }
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}