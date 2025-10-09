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
            _logger.LogInformation("Iniciando criação de funcionário. Nome: {Nome}, PortadorAcesso: {PortadorAcesso}", 
                funcionario.NomeFuncionario, funcionario.PortadorAcesso);

            try
            {
                funcionario.Id = Guid.NewGuid().ToString();
                _context.Funcionarios.Add(funcionario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Funcionário criado com sucesso. ID: {Id}, Nome: {Nome}", 
                    funcionario.Id, funcionario.NomeFuncionario);
                
                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar funcionário. Nome: {Nome}, PortadorAcesso: {PortadorAcesso}", 
                    funcionario.NomeFuncionario, funcionario.PortadorAcesso);
                throw;
            }
        }

        public async Task<Funcionario?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando funcionário por ID: {Id}", id);

            try
            {
                var funcionario = await _context.Funcionarios
                                     .FirstOrDefaultAsync(f => f.Id == id);

                if (funcionario == null)
                {
                    _logger.LogWarning("Funcionário não encontrado para ID: {Id}", id);
                }
                else
                {
                    _logger.LogInformation("Funcionário encontrado. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                        funcionario.Id, funcionario.NomeFuncionario, funcionario.IsActive);
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar funcionário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Funcionario?> GetByPortadorAcessoAsync(string portadorAcesso)
        {
            _logger.LogInformation("Buscando funcionário por portador de acesso: {PortadorAcesso}", portadorAcesso);

            try
            {
                var funcionario = await _context.Funcionarios
                                     .FirstOrDefaultAsync(f => f.PortadorAcesso == portadorAcesso);

                if (funcionario == null)
                {
                    _logger.LogWarning("Funcionário não encontrado para portador de acesso: {PortadorAcesso}", portadorAcesso);
                }
                else
                {
                    _logger.LogInformation("Funcionário encontrado por portador de acesso. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                        funcionario.Id, funcionario.NomeFuncionario, funcionario.IsActive);
                }

                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar funcionário por portador de acesso: {PortadorAcesso}", portadorAcesso);
                throw;
            }
        }

        public async Task<IEnumerable<Funcionario>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os funcionários ativos");

            try
            {
                var funcionarios = await _context.Funcionarios
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
            _logger.LogInformation("Atualizando funcionário. ID: {Id}, Nome: {Nome}", 
                funcionario.Id, funcionario.NomeFuncionario);

            try
            {
                _context.Funcionarios.Update(funcionario);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Funcionário atualizado com sucesso. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                    funcionario.Id, funcionario.NomeFuncionario, funcionario.IsActive);
                
                return funcionario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar funcionário. ID: {Id}, Nome: {Nome}", 
                    funcionario.Id, funcionario.NomeFuncionario);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de funcionário. ID: {Id}", id);

            try
            {
                var funcionario = await GetByIdAsync(id);
                if (funcionario == null)
                {
                    _logger.LogWarning("Tentativa de desativar funcionário inexistente. ID: {Id}", id);
                    return false;
                }

                funcionario.IsActive = false;
                await UpdateAsync(funcionario);
                
                _logger.LogInformation("Funcionário desativado com sucesso. ID: {Id}, Nome: {Nome}", 
                    funcionario.Id, funcionario.NomeFuncionario);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar funcionário. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> PortadorAcessoExistsAsync(string portadorAcesso)
        {
            _logger.LogInformation("Verificando se portador de acesso já existe: {PortadorAcesso}", portadorAcesso);

            try
            {
                var exists = await _context.Funcionarios
                                   .AnyAsync(f => f.PortadorAcesso == portadorAcesso);

                _logger.LogInformation("Verificação de portador de acesso concluída. PortadorAcesso: {PortadorAcesso}, Existe: {Exists}", 
                    portadorAcesso, exists);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se portador de acesso existe: {PortadorAcesso}", portadorAcesso);
                throw;
            }
        }
    }
}