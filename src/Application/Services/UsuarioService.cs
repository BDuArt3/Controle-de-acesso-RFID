using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(IUsuarioRepository usuarioRepository, ILogger<UsuarioService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<UsuarioDto> CreateAsync(CreateUsuarioDto createUsuarioDto)
        {
            _logger.LogInformation("Iniciando criação de usuário. Nome: {Nome}, Tipo: {Tipo}", 
                createUsuarioDto.Nome, createUsuarioDto.Tipo);

            try
            {
                if (string.IsNullOrWhiteSpace(createUsuarioDto.Nome))
                {
                    throw new ArgumentException("Nome do usuário é obrigatório", nameof(createUsuarioDto.Nome));
                }

                var usuario = new Usuario
                {
                    Nome = createUsuarioDto.Nome.Trim(),
                    Email = NormalizeEmail(createUsuarioDto.Email),
                    Documento = createUsuarioDto.Documento?.Trim() ?? string.Empty,
                    Tipo = createUsuarioDto.Tipo,
                    IsActive = true
                };
                usuario.HistoricosStatus.Add(new HistoricoStatusUsuario
                {
                    IsActive = true,
                    DataHora = DateTime.UtcNow,
                    Motivo = "Cadastro inicial."
                });

                if (usuario.Email is null)
                {
                    throw new ArgumentException("Email é obrigatório.", nameof(createUsuarioDto.Email));
                }

                ValidateEmail(usuario.Email);
                if (await _usuarioRepository.EmailExistsAsync(usuario.Email))
                {
                    throw new InvalidOperationException("Email já está em uso.");
                }

                var usuarioCriado = await _usuarioRepository.CreateAsync(usuario);

                _logger.LogInformation("Usuário criado com sucesso. ID: {Id}, Nome: {Nome}", 
                    usuarioCriado.Id, usuarioCriado.Nome);

                return MapToDto(usuarioCriado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuário. Nome: {Nome}, Tipo: {Tipo}", 
                    createUsuarioDto.Nome, createUsuarioDto.Tipo);
                throw;
            }
        }

        public async Task<UsuarioDto?> GetByIdAsync(string id)
        {
            _logger.LogInformation("Buscando usuário por ID: {Id}", id);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                var usuario = await _usuarioRepository.GetByIdAsync(id);

                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado para ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Usuário encontrado. ID: {Id}, Nome: {Nome}", usuario.Id, usuario.Nome);

                return MapToDto(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<UsuarioDto>> GetAllAsync()
        {
            _logger.LogInformation("Buscando todos os usuários ativos");

            try
            {
                var usuarios = await _usuarioRepository.GetAllAsync();

                _logger.LogInformation("Encontrados {Count} usuários ativos", usuarios.Count());

                return usuarios.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os usuários");
                throw;
            }
        }

        public async Task<UsuarioDto> UpdateAsync(string id, CreateUsuarioDto updateUsuarioDto)
        {
            _logger.LogInformation("Atualizando usuário. ID: {Id}, Nome: {Nome}", id, updateUsuarioDto.Nome);

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("ID é obrigatório", nameof(id));
                }

                if (string.IsNullOrWhiteSpace(updateUsuarioDto.Nome))
                {
                    throw new ArgumentException("Nome do usuário é obrigatório", nameof(updateUsuarioDto.Nome));
                }

                var usuarioExistente = await _usuarioRepository.GetByIdAsync(id);
                if (usuarioExistente == null)
                {
                    throw new KeyNotFoundException($"Usuário com ID {id} não encontrado");
                }

                usuarioExistente.Nome = updateUsuarioDto.Nome.Trim();
                usuarioExistente.Email = NormalizeEmail(updateUsuarioDto.Email);
                if (!string.IsNullOrWhiteSpace(updateUsuarioDto.Documento))
                {
                    usuarioExistente.Documento = updateUsuarioDto.Documento.Trim();
                }
                usuarioExistente.Tipo = updateUsuarioDto.Tipo;

                if (usuarioExistente.Email is null)
                {
                    throw new ArgumentException("Email é obrigatório.", nameof(updateUsuarioDto.Email));
                }

                ValidateEmail(usuarioExistente.Email);
                if (await _usuarioRepository.EmailExistsAsync(usuarioExistente.Email, id))
                {
                    throw new InvalidOperationException("Email já está em uso.");
                }

                var usuarioAtualizado = await _usuarioRepository.UpdateAsync(usuarioExistente);

                _logger.LogInformation("Usuário atualizado com sucesso. ID: {Id}, Nome: {Nome}", 
                    usuarioAtualizado.Id, usuarioAtualizado.Nome);

                return MapToDto(usuarioAtualizado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar usuário. ID: {Id}, Nome: {Nome}", id, updateUsuarioDto.Nome);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, string funcionarioResponsavelId, string motivo)
        {
            _logger.LogInformation("Iniciando desativação de usuário. ID: {Id}", id);

            try
            {
                ValidateStatusChange(id, funcionarioResponsavelId, motivo);

                var usuario = await _usuarioRepository.GetByIdAsync(id);
                if (usuario is null)
                {
                    _logger.LogWarning("Usuário não encontrado para desativação. ID: {Id}", id);
                    return false;
                }

                if (usuario.IsActive)
                {
                    usuario.IsActive = false;
                    usuario.HistoricosStatus.Add(new HistoricoStatusUsuario
                    {
                        IsActive = false,
                        DataHora = DateTime.UtcNow,
                        Motivo = motivo.Trim(),
                        AlteradoPorFuncionarioId = funcionarioResponsavelId
                    });
                    await _usuarioRepository.UpdateAsync(usuario);
                }

                _logger.LogInformation("Usuário desativado com sucesso. ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar usuário. ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ActivateAsync(string id, string funcionarioResponsavelId, string motivo)
        {
            _logger.LogInformation("Iniciando ativação de usuário. ID: {Id}", id);

            try
            {
                ValidateStatusChange(id, funcionarioResponsavelId, motivo);

                var usuario = await _usuarioRepository.GetByIdAsync(id);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado para ativação. ID: {Id}", id);
                    return false;
                }

                if (!usuario.IsActive)
                {
                    usuario.IsActive = true;
                    usuario.HistoricosStatus.Add(new HistoricoStatusUsuario
                    {
                        IsActive = true,
                        DataHora = DateTime.UtcNow,
                        Motivo = motivo.Trim(),
                        AlteradoPorFuncionarioId = funcionarioResponsavelId
                    });
                    await _usuarioRepository.UpdateAsync(usuario);
                }

                _logger.LogInformation("Usuário ativado com sucesso. ID: {Id}, Nome: {Nome}", 
                    usuario.Id, usuario.Nome);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ativar usuário. ID: {Id}", id);
                throw;
            }
        }

        private static void ValidateStatusChange(string id, string funcionarioResponsavelId, string motivo)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID é obrigatório.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(funcionarioResponsavelId))
            {
                throw new ArgumentException("Funcionário responsável é obrigatório.", nameof(funcionarioResponsavelId));
            }

            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 200)
            {
                throw new ArgumentException("O motivo é obrigatório e deve ter até 200 caracteres.", nameof(motivo));
            }
        }

        private static UsuarioDto MapToDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Tipo = usuario.Tipo,
                IsActive = usuario.IsActive,
                Cartoes = usuario.Cartoes?.Select(c => new CartaoDto
                {
                    Id = c.Id,
                    Uid = c.Uid,
                    Status = c.Status,
                    IsActive = c.Status == StatusCartao.Ativo,
                    UsuarioId = c.UsuarioId
                }).ToList() ?? new List<CartaoDto>()
            };
        }

        private static string RequiredTrimmed(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Campo obrigatório.", parameterName);
            }

            return value.Trim();
        }

        private static string? NormalizeEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        private static void ValidateEmail(string email)
        {
            if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            {
                throw new ArgumentException("Email inválido ou maior que 254 caracteres.", nameof(email));
            }
        }
    }
}