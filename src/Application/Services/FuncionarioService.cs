using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class FuncionarioService : IFuncionarioService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly ILogger<FuncionarioService> _logger;

        public FuncionarioService(IFuncionarioRepository funcionarioRepository, ILogger<FuncionarioService> logger)
        {
            _funcionarioRepository = funcionarioRepository;
            _logger = logger;
        }

        public async Task<FuncionarioDto> CreateAsync(CreateFuncionarioDto funcionarioDto)
        {
            _logger.LogInformation("Iniciando criação de funcionário. Nome: {Nome}, PortadorAcesso: {PortadorAcesso}, Tipo: {Tipo}", 
                funcionarioDto.NomeFuncionario, funcionarioDto.PortadorAcesso, funcionarioDto.Tipo);

            try
            {
                // Validação: verificar se o portador de acesso já existe
                if (await _funcionarioRepository.PortadorAcessoExistsAsync(funcionarioDto.PortadorAcesso))
                {
                    _logger.LogWarning("Tentativa de criar funcionário com portador de acesso já existente: {PortadorAcesso}", 
                        funcionarioDto.PortadorAcesso);
                    throw new InvalidOperationException("Portador de acesso já existe.");
                }

                // Criptografar a senha
                var senhaHash = BCrypt.Net.BCrypt.HashPassword(funcionarioDto.Senha);
                _logger.LogInformation("Senha criptografada com sucesso para o funcionário: {PortadorAcesso}", 
                    funcionarioDto.PortadorAcesso);

                var funcionario = new Funcionario
                {
                    NomeFuncionario = funcionarioDto.NomeFuncionario,
                    PortadorAcesso = funcionarioDto.PortadorAcesso,
                    SenhaHash = senhaHash,
                    Tipo = funcionarioDto.Tipo,
                    IsActive = true
                };

                var novoFuncionario = await _funcionarioRepository.CreateAsync(funcionario);

                _logger.LogInformation("Funcionário criado com sucesso. ID: {Id}, Nome: {Nome}, PortadorAcesso: {PortadorAcesso}", 
                    novoFuncionario.Id, novoFuncionario.NomeFuncionario, novoFuncionario.PortadorAcesso);

                return new FuncionarioDto
                {
                    Id = novoFuncionario.Id,
                    NomeFuncionario = novoFuncionario.NomeFuncionario,
                    PortadorAcesso = novoFuncionario.PortadorAcesso,
                    Tipo = novoFuncionario.Tipo,
                    IsActive = novoFuncionario.IsActive
                };
            }
            catch (InvalidOperationException)
            {
                // Re-throw validation exceptions without logging as error (already logged as warning)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar funcionário. Nome: {Nome}, PortadorAcesso: {PortadorAcesso}", 
                    funcionarioDto.NomeFuncionario, funcionarioDto.PortadorAcesso);
                throw;
            }
        }

        public async Task<FuncionarioDto?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando funcionário por ID: {Id}", id);

            try
            {
                var funcionario = await _funcionarioRepository.GetByIdAsync(id);

                if (funcionario == null)
                {
                    _logger.LogWarning("Funcionário não encontrado para ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Funcionário encontrado com sucesso. ID: {Id}, Nome: {Nome}, Ativo: {IsActive}", 
                    funcionario.Id, funcionario.NomeFuncionario, funcionario.IsActive);

                return new FuncionarioDto
                {
                    Id = funcionario.Id,
                    NomeFuncionario = funcionario.NomeFuncionario,
                    PortadorAcesso = funcionario.PortadorAcesso,
                    Tipo = funcionario.Tipo,
                    IsActive = funcionario.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar funcionário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<FuncionarioDto>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os funcionários ativos");

            try
            {
                var funcionarios = await _funcionarioRepository.GetAllAsync();
                var funcionarioDtos = funcionarios.Select(f => new FuncionarioDto
                {
                    Id = f.Id,
                    NomeFuncionario = f.NomeFuncionario,
                    PortadorAcesso = f.PortadorAcesso,
                    Tipo = f.Tipo,
                    IsActive = f.IsActive
                }).ToList();

                _logger.LogInformation("Retornando {Count} funcionários ativos", funcionarioDtos.Count);

                return funcionarioDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os funcionários");
                throw;
            }
        }

        public async Task<FuncionarioDto> UpdateAsync(string id, CreateFuncionarioDto funcionarioDto)
        {
            _logger.LogInformation("Iniciando atualização de funcionário. ID: {Id}, Nome: {Nome}", 
                id, funcionarioDto.NomeFuncionario);

            try
            {
                var funcionarioExistente = await _funcionarioRepository.GetByIdAsync(id);
                if (funcionarioExistente == null)
                {
                    _logger.LogWarning("Tentativa de atualizar funcionário inexistente. ID: {Id}", id);
                    throw new InvalidOperationException("Funcionário não encontrado.");
                }

                // Verificar se o novo portador de acesso já existe (exceto para o próprio funcionário)
                var funcionarioComMesmoPortador = await _funcionarioRepository.GetByPortadorAcessoAsync(funcionarioDto.PortadorAcesso);
                if (funcionarioComMesmoPortador != null && funcionarioComMesmoPortador.Id != id)
                {
                    _logger.LogWarning("Tentativa de atualizar funcionário com portador de acesso já existente: {PortadorAcesso}", 
                        funcionarioDto.PortadorAcesso);
                    throw new InvalidOperationException("Portador de acesso já existe.");
                }

                // Atualizar dados
                funcionarioExistente.NomeFuncionario = funcionarioDto.NomeFuncionario;
                funcionarioExistente.PortadorAcesso = funcionarioDto.PortadorAcesso;
                funcionarioExistente.Tipo = funcionarioDto.Tipo;

                // Atualizar senha apenas se uma nova senha foi fornecida
                if (!string.IsNullOrEmpty(funcionarioDto.Senha))
                {
                    funcionarioExistente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(funcionarioDto.Senha);
                    _logger.LogInformation("Senha atualizada para o funcionário: {Id}", id);
                }

                var funcionarioAtualizado = await _funcionarioRepository.UpdateAsync(funcionarioExistente);

                _logger.LogInformation("Funcionário atualizado com sucesso. ID: {Id}, Nome: {Nome}", 
                    funcionarioAtualizado.Id, funcionarioAtualizado.NomeFuncionario);

                return new FuncionarioDto
                {
                    Id = funcionarioAtualizado.Id,
                    NomeFuncionario = funcionarioAtualizado.NomeFuncionario,
                    PortadorAcesso = funcionarioAtualizado.PortadorAcesso,
                    Tipo = funcionarioAtualizado.Tipo,
                    IsActive = funcionarioAtualizado.IsActive
                };
            }
            catch (InvalidOperationException)
            {
                // Re-throw validation exceptions without logging as error (already logged as warning)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar funcionário. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            _logger.LogInformation("Iniciando desativação de funcionário. ID: {Id}", id);

            try
            {
                var resultado = await _funcionarioRepository.DeleteAsync(id);

                if (resultado)
                {
                    _logger.LogInformation("Funcionário desativado com sucesso. ID: {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Tentativa de desativar funcionário inexistente. ID: {Id}", id);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar funcionário. ID: {Id}", id);
                throw;
            }
        }
    }
}