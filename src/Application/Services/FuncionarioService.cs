using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging;

namespace ControleDeAcesso.Application.Services
{
    public class FuncionarioService : IFuncionarioService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FuncionarioService> _logger;

        public FuncionarioService(
            IFuncionarioRepository funcionarioRepository,
            IUsuarioRepository usuarioRepository,
            IUnitOfWork unitOfWork,
            ILogger<FuncionarioService> logger)
        {
            _funcionarioRepository = funcionarioRepository;
            _usuarioRepository = usuarioRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<FuncionarioDto> CreateAsync(CreateFuncionarioDto funcionarioDto)
        {
            ArgumentNullException.ThrowIfNull(funcionarioDto);

            var nome = RequiredTrimmed(funcionarioDto.NomeFuncionario, nameof(funcionarioDto.NomeFuncionario));
            var email = NormalizeEmail(funcionarioDto.Email);
            var documento = RequiredTrimmed(funcionarioDto.Cpf, nameof(funcionarioDto.Cpf));
            ValidateEmail(email);
            ValidateFuncionarioType(funcionarioDto.Tipo);

            if (string.IsNullOrWhiteSpace(funcionarioDto.Senha))
            {
                throw new ArgumentException("Senha é obrigatória para criar funcionário.", nameof(funcionarioDto.Senha));
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    if (await _usuarioRepository.EmailExistsAsync(email))
                    {
                        throw new InvalidOperationException("Email de acesso já está em uso.");
                    }

                    var usuario = await _usuarioRepository.CreateAsync(new Usuario
                    {
                        Nome = nome,
                        Email = email,
                        Documento = documento,
                        Tipo = TipoUsuario.Funcionario,
                        IsActive = true
                    });

                    var funcionario = new Funcionario
                    {
                        UsuarioId = usuario.Id,
                        Usuario = usuario,
                        SenhaHash = BCrypt.Net.BCrypt.HashPassword(funcionarioDto.Senha),
                        Tipo = funcionarioDto.Tipo,
                        IsActive = true
                    };

                    funcionario.HistoricosStatusFuncionario.Add(new HistoricoStatusFuncionario
                    {
                        IsActive = true,
                        DataHora = DateTime.UtcNow,
                        Motivo = "Cadastro inicial."
                    });
                    usuario.HistoricosStatus.Add(new HistoricoStatusUsuario
                    {
                        IsActive = true,
                        DataHora = DateTime.UtcNow,
                        Motivo = "Cadastro inicial."
                    });

                    var criado = await _funcionarioRepository.CreateAsync(funcionario);
                    _logger.LogInformation("Funcionário criado. ID: {FuncionarioId}", criado.Id);
                    return MapToDto(criado);
                });
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar funcionário vinculado ao usuário.");
                throw;
            }
        }

        public async Task<FuncionarioDto?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID do funcionário é obrigatório.", nameof(id));
            }

            var funcionario = await _funcionarioRepository.GetByIdAsync(id);
            return funcionario is null ? null : MapToDto(funcionario);
        }

        public async Task<IEnumerable<FuncionarioDto>> GetAllAsync()
        {
            var funcionarios = await _funcionarioRepository.GetAllAsync();
            return funcionarios.Select(MapToDto).ToList();
        }

        public async Task<FuncionarioDto> UpdateAsync(string id, CreateFuncionarioDto funcionarioDto)
        {
            ArgumentNullException.ThrowIfNull(funcionarioDto);

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID do funcionário é obrigatório.", nameof(id));
            }

            var nome = RequiredTrimmed(funcionarioDto.NomeFuncionario, nameof(funcionarioDto.NomeFuncionario));
            var email = NormalizeEmail(funcionarioDto.Email);
            var documento = RequiredTrimmed(funcionarioDto.Cpf, nameof(funcionarioDto.Cpf));
            ValidateEmail(email);
            ValidateFuncionarioType(funcionarioDto.Tipo);

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var funcionario = await _funcionarioRepository.GetByIdAsync(id);
                if (funcionario is null)
                {
                    throw new KeyNotFoundException("Funcionário não encontrado.");
                }

                if (!string.Equals(funcionario.Usuario.Email, email, StringComparison.Ordinal) &&
                    await _usuarioRepository.EmailExistsAsync(email, funcionario.UsuarioId))
                {
                    throw new InvalidOperationException("Email de acesso já está em uso.");
                }

                funcionario.Usuario.Nome = nome;
                funcionario.Usuario.Email = email;
                funcionario.Usuario.Documento = documento;
                funcionario.Tipo = funcionarioDto.Tipo;

                if (!string.IsNullOrWhiteSpace(funcionarioDto.Senha))
                {
                    funcionario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(funcionarioDto.Senha);
                }

                var atualizado = await _funcionarioRepository.UpdateAsync(funcionario);
                return MapToDto(atualizado);
            });
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID do funcionário é obrigatório.", nameof(id));
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var funcionario = await _funcionarioRepository.GetByIdAsync(id);
                if (funcionario is null)
                {
                    return false;
                }

                if (funcionario.IsActive)
                {
                    funcionario.IsActive = false;
                    funcionario.HistoricosStatusFuncionario.Add(new HistoricoStatusFuncionario
                    {
                        IsActive = false,
                        DataHora = DateTime.UtcNow,
                        Motivo = "Desativação da conta."
                    });
                    await _funcionarioRepository.UpdateAsync(funcionario);
                }

                return true;
            });
        }

        public async Task<bool> ActivateAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID do funcionário é obrigatório.", nameof(id));
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var funcionario = await _funcionarioRepository.GetByIdAsync(id);
                if (funcionario is null)
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                var changed = false;
                if (!funcionario.IsActive)
                {
                    funcionario.IsActive = true;
                    changed = true;
                    funcionario.HistoricosStatusFuncionario.Add(new HistoricoStatusFuncionario
                    {
                        IsActive = true,
                        DataHora = now,
                        Motivo = "Reativação da conta."
                    });
                }

                if (changed)
                {
                    await _funcionarioRepository.UpdateAsync(funcionario);
                }
                return true;
            });
        }

        private static FuncionarioDto MapToDto(Funcionario funcionario) => new()
        {
            Id = funcionario.Id,
            NomeFuncionario = funcionario.Usuario.Nome,
            Email = funcionario.Usuario.Email ?? string.Empty,
            Tipo = funcionario.Tipo,
            IsActive = funcionario.IsActive
        };

        private static string RequiredTrimmed(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Campo obrigatório.", parameterName);
            }

            return value.Trim();
        }

        private static string NormalizeEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

        private static void ValidateEmail(string email)
        {
            if (email.Length > 254 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                throw new ArgumentException("Email inválido ou maior que 254 caracteres.", nameof(email));
            }
        }

        private static void ValidateFuncionarioType(TipoFuncionario tipo)
        {
            if (!Enum.IsDefined(tipo))
            {
                throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo de funcionário inválido.");
            }
        }
    }
}
