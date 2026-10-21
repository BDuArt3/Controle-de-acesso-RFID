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
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly ISolicitacaoGravacaoCartaoRepository _solicitacaoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CartaoService> _logger;

        public CartaoService(
            ICartaoRepository cartaoRepository,
            IUsuarioRepository usuarioRepository,
            IFuncionarioRepository funcionarioRepository,
            ISolicitacaoGravacaoCartaoRepository solicitacaoRepository,
            IUnitOfWork unitOfWork,
            ILogger<CartaoService> logger)
        {
            _cartaoRepository = cartaoRepository;
            _usuarioRepository = usuarioRepository;
            _funcionarioRepository = funcionarioRepository;
            _solicitacaoRepository = solicitacaoRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GravacaoSolicitadaResult> SolicitarGravacaoAsync(
            GravarCartaoDto request,
            string funcionarioId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var usuarioId = request.UsuarioId?.Trim();
            var solicitanteId = funcionarioId?.Trim();
            var chave = idempotencyKey?.Trim();
            var cartaoAnteriorId = request.CartaoAnteriorId?.Trim();
            var motivoReemissao = request.MotivoReemissao?.Trim();

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new ArgumentException("ID do usuário é obrigatório.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(solicitanteId))
            {
                throw new ArgumentException("Funcionário solicitante inválido.", nameof(funcionarioId));
            }

            if (string.IsNullOrWhiteSpace(chave) || chave.Length > 128)
            {
                throw new ArgumentException("O header Idempotency-Key é obrigatório e deve ter até 128 caracteres.", nameof(idempotencyKey));
            }

            if (cartaoAnteriorId != null && string.IsNullOrWhiteSpace(motivoReemissao))
            {
                throw new ArgumentException("Informe o motivo da reemissão.", nameof(request));
            }

            if (motivoReemissao?.Length > 200)
            {
                throw new ArgumentException("O motivo da reemissão deve ter até 200 caracteres.", nameof(request));
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var solicitacaoExistente = await _solicitacaoRepository.GetByIdempotencyKeyAsync(
                    solicitanteId, chave, cancellationToken);
                if (solicitacaoExistente != null)
                {
                    if (solicitacaoExistente.UsuarioId != usuarioId ||
                        solicitacaoExistente.CartaoAnteriorId != cartaoAnteriorId ||
                        solicitacaoExistente.MotivoReemissao != motivoReemissao)
                    {
                        throw new InvalidOperationException(
                            "A chave Idempotency-Key já foi usada com dados diferentes.");
                    }

                    var usuarioRepetido = await _usuarioRepository.GetByIdAsync(usuarioId);
                    return new GravacaoSolicitadaResult(
                        await MapSolicitacaoAsync(solicitacaoExistente, usuarioRepetido?.Nome),
                        CriarComando(solicitacaoExistente));
                }

                var solicitante = await _funcionarioRepository.GetByIdAsync(solicitanteId);
                if (solicitante == null || !solicitante.IsActive)
                {
                    throw new InvalidOperationException("O funcionário solicitante não existe ou está inativo.");
                }

                var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
                if (usuario == null)
                {
                    throw new KeyNotFoundException($"Usuário com ID {usuarioId} não encontrado.");
                }

                if (!usuario.IsActive)
                {
                    throw new InvalidOperationException($"Usuário com ID {usuarioId} está inativo.");
                }

                Cartao? cartaoAnterior = null;
                if (cartaoAnteriorId != null)
                {
                    cartaoAnterior = await _cartaoRepository.GetByIdAsync(cartaoAnteriorId);
                    if (cartaoAnterior == null)
                    {
                        throw new KeyNotFoundException($"Cartão anterior com ID {cartaoAnteriorId} não encontrado.");
                    }

                    if (cartaoAnterior.UsuarioId != usuarioId)
                    {
                        throw new InvalidOperationException("O cartão anterior não pertence ao usuário selecionado.");
                    }

                    if (cartaoAnterior.Status != StatusCartao.Ativo)
                    {
                        throw new InvalidOperationException("Somente um cartão ativo pode iniciar uma reemissão.");
                    }

                    if (await _solicitacaoRepository.HasOpenRequestForCardAsync(cartaoAnteriorId, cancellationToken))
                    {
                        throw new InvalidOperationException("Já existe uma reemissão pendente para este cartão.");
                    }
                }

                var cartoesAtivos = await _cartaoRepository.GetByUsuarioIdAsync(usuarioId);
                if (cartoesAtivos.Any(cartao => cartao.Id != cartaoAnteriorId))
                {
                    throw new InvalidOperationException(
                        "O usuário já possui um cartão ativo. Bloqueie ou selecione o cartão atual para reemissão.");
                }

                var credencial = await GerarCredencialUnicaAsync(cancellationToken);
                var agora = DateTime.UtcNow;
                var solicitacao = new SolicitacaoGravacaoCartao
                {
                    ChaveIdempotencia = chave,
                    Credencial = credencial,
                    UsuarioId = usuarioId,
                    SolicitanteFuncionarioId = solicitanteId,
                    CartaoAnteriorId = cartaoAnteriorId,
                    MotivoReemissao = motivoReemissao,
                    DataSolicitacao = agora
                };

                if (cartaoAnterior != null)
                {
                    if (!cartaoAnterior.AlterarStatus(StatusCartao.Bloqueado))
                    {
                        throw new InvalidOperationException("O cartão anterior já não está ativo.");
                    }

                    await _cartaoRepository.UpdateStatusWithHistoryAsync(
                        cartaoAnterior,
                        new HistoricoStatusCartao
                        {
                            CartaoId = cartaoAnterior.Id,
                            Status = StatusCartao.Bloqueado,
                            DataHora = agora,
                            Motivo = TruncarMotivo($"Bloqueado para reemissão: {motivoReemissao}"),
                            AlteradoPorFuncionarioId = solicitanteId
                        });
                }

                await _solicitacaoRepository.AddAsync(solicitacao, cancellationToken);
                _logger.LogInformation(
                    "Solicitação de gravação persistida. Solicitação: {SolicitacaoId}, Usuário: {UsuarioId}, Reemissão: {Reemissao}",
                    solicitacao.Id, usuarioId, cartaoAnteriorId != null);

                return new GravacaoSolicitadaResult(
                    await MapSolicitacaoAsync(solicitacao, usuario.Nome),
                    CriarComando(solicitacao));
            });
        }

        public async Task<GravacaoCartaoResponseDto?> GetSolicitacaoAsync(
            string id,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID da solicitação é obrigatório.", nameof(id));
            }

            var solicitacao = await _solicitacaoRepository.GetByIdAsync(id, cancellationToken);
            if (solicitacao == null)
            {
                return null;
            }

            var usuario = await _usuarioRepository.GetByIdAsync(solicitacao.UsuarioId);
            return await MapSolicitacaoAsync(solicitacao, usuario?.Nome);
        }

        public async Task<GravacaoCartaoCommandDto?> PrepararRetentativaAsync(
            string id,
            CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitacao = await _solicitacaoRepository.GetByIdAsync(id, cancellationToken);
                if (solicitacao == null)
                {
                    return null;
                }

                if (!solicitacao.PrepararRetentativa(DateTime.UtcNow, TimeSpan.FromMinutes(1)))
                {
                    throw new InvalidOperationException(
                        "A solicitação não pode ser reenviada agora. Aguarde a falha do Worker ou pelo menos um minuto após o último despacho.");
                }

                await _solicitacaoRepository.SaveChangesAsync(cancellationToken);
                return CriarComando(solicitacao);
            });
        }

        public async Task<bool> RegistrarDespachoAsync(string id, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitacao = await _solicitacaoRepository.GetByIdAsync(id, cancellationToken);
                if (solicitacao == null)
                {
                    return false;
                }

                if (!solicitacao.MarcarEnviada(DateTime.UtcNow))
                {
                    return false;
                }

                await _solicitacaoRepository.SaveChangesAsync(cancellationToken);
                return true;
            });
        }

        public async Task<GravacaoCartaoResponseDto?> ConfirmarGravacaoAsync(
            ConfirmarGravacaoCartaoDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var solicitacaoId = request.SolicitacaoId?.Trim();
            var credencial = request.CredencialGravada?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(solicitacaoId) ||
                string.IsNullOrWhiteSpace(credencial) ||
                credencial.Length != 16 ||
                credencial.Any(character => !Uri.IsHexDigit(character)))
            {
                throw new ArgumentException("Solicitação e credencial hexadecimal de 16 caracteres são obrigatórias.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitacao = await _solicitacaoRepository.GetByIdAsync(solicitacaoId, cancellationToken);
                if (solicitacao == null)
                {
                    return null;
                }

                if (!string.Equals(solicitacao.Credencial, credencial, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A credencial confirmada não corresponde à solicitação.");
                }

                var usuario = await _usuarioRepository.GetByIdAsync(solicitacao.UsuarioId);
                if (solicitacao.Status == StatusSolicitacaoGravacao.Concluida)
                {
                    return await MapSolicitacaoAsync(solicitacao, usuario?.Nome);
                }

                if (solicitacao.Status == StatusSolicitacaoGravacao.Cancelada)
                {
                    throw new InvalidOperationException("A solicitação foi cancelada e não pode ser confirmada.");
                }

                if (usuario == null || !usuario.IsActive)
                {
                    solicitacao.MarcarFalha("Usuário destinatário inexistente ou inativo na confirmação.");
                    await _solicitacaoRepository.SaveChangesAsync(cancellationToken);
                    return await MapSolicitacaoAsync(solicitacao, usuario?.Nome);
                }

                var cartaoExistente = await _cartaoRepository.GetByUidAsync(credencial);
                if (cartaoExistente != null)
                {
                    if (cartaoExistente.Id == solicitacao.CartaoCriadoId &&
                        solicitacao.Status == StatusSolicitacaoGravacao.Concluida)
                    {
                        return await MapSolicitacaoAsync(solicitacao, usuario.Nome);
                    }

                    throw new InvalidOperationException("A credencial já pertence a outro cartão lógico.");
                }

                Cartao? cartaoAnterior = null;
                if (solicitacao.CartaoAnteriorId != null)
                {
                    cartaoAnterior = await _cartaoRepository.GetByIdAsync(solicitacao.CartaoAnteriorId);
                    if (cartaoAnterior == null || cartaoAnterior.Status != StatusCartao.Bloqueado)
                    {
                        throw new InvalidOperationException(
                            "O cartão anterior não está no estado bloqueado esperado para concluir a reemissão.");
                    }

                    if (cartaoAnterior.HistoricoAssociacaoCartao == null ||
                        cartaoAnterior.HistoricoAssociacaoCartao.DataDesassociacao != null)
                    {
                        throw new InvalidOperationException(
                            "O histórico de associação do cartão anterior está ausente ou já encerrado.");
                    }
                }

                if ((await _cartaoRepository.GetByUsuarioIdAsync(usuario.Id)).Any())
                {
                    throw new InvalidOperationException(
                        "O usuário já possui um cartão ativo. A gravação não pode ser confirmada.");
                }

                var agora = DateTime.UtcNow;
                var novoCartao = new Cartao
                {
                    Uid = credencial,
                    UsuarioId = usuario.Id,
                    DataRegistro = agora,
                    DataExpiracao = usuario.Tipo == TipoUsuario.Visitante
                        ? agora.AddHours(24)
                        : null
                };
                var associacao = new HistoricoAssociacoesCartao
                {
                    CartaoId = novoCartao.Id,
                    DataAssociacao = agora,
                    AssociadoPorFuncionarioId = solicitacao.SolicitanteFuncionarioId
                };
                var statusInicial = new HistoricoStatusCartao
                {
                    CartaoId = novoCartao.Id,
                    Status = StatusCartao.Ativo,
                    DataHora = agora,
                    Motivo = solicitacao.CartaoAnteriorId == null
                        ? "Emissão inicial do cartão."
                        : "Nova emissão após regravação do cartão físico.",
                    AlteradoPorFuncionarioId = solicitacao.SolicitanteFuncionarioId
                };

                await _cartaoRepository.CreateWithAuditAsync(novoCartao, associacao, statusInicial);

                if (cartaoAnterior != null)
                {
                    cartaoAnterior.AlterarStatus(StatusCartao.Cancelado);
                    cartaoAnterior.HistoricoAssociacaoCartao!.DataDesassociacao = agora;
                    cartaoAnterior.HistoricoAssociacaoCartao.DesassociadoPorFuncionarioId =
                        solicitacao.SolicitanteFuncionarioId;

                    await _cartaoRepository.UpdateStatusWithHistoryAsync(
                        cartaoAnterior,
                        new HistoricoStatusCartao
                        {
                            CartaoId = cartaoAnterior.Id,
                            Status = StatusCartao.Cancelado,
                            DataHora = agora,
                            Motivo = "Emissão cancelada após confirmação da nova gravação.",
                            AlteradoPorFuncionarioId = solicitacao.SolicitanteFuncionarioId
                        });
                }

                solicitacao.MarcarConcluida(novoCartao.Id, agora);
                await _solicitacaoRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Gravação confirmada. Solicitação: {SolicitacaoId}, Cartão criado: {CartaoId}, Cartão anterior: {CartaoAnteriorId}",
                    solicitacao.Id, novoCartao.Id, solicitacao.CartaoAnteriorId);

                return await MapSolicitacaoAsync(solicitacao, usuario.Nome);
            });
        }

        public async Task<GravacaoCartaoResponseDto?> RegistrarFalhaGravacaoAsync(
            FalharGravacaoCartaoDto request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var motivo = request.Motivo?.Trim();
            if (string.IsNullOrWhiteSpace(request.SolicitacaoId) ||
                string.IsNullOrWhiteSpace(motivo) ||
                motivo.Length > 200)
            {
                throw new ArgumentException("Solicitação e motivo de falha válidos são obrigatórios.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitacao = await _solicitacaoRepository.GetByIdAsync(
                    request.SolicitacaoId.Trim(), cancellationToken);
                if (solicitacao == null)
                {
                    return null;
                }

                if (solicitacao.Status == StatusSolicitacaoGravacao.Cancelada)
                {
                    throw new InvalidOperationException("A solicitação foi cancelada.");
                }

                if (solicitacao.MarcarFalha(motivo))
                {
                    await _solicitacaoRepository.SaveChangesAsync(cancellationToken);
                }

                var usuario = await _usuarioRepository.GetByIdAsync(solicitacao.UsuarioId);
                return await MapSolicitacaoAsync(solicitacao, usuario?.Nome);
            });
        }

        public async Task<GravacaoCartaoResponseDto?> CancelarSolicitacaoAsync(
            string id,
            string funcionarioId,
            string motivo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(funcionarioId) ||
                string.IsNullOrWhiteSpace(motivo) ||
                motivo.Trim().Length > 200)
            {
                throw new ArgumentException("Solicitação, funcionário e motivo são obrigatórios.");
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitacao = await _solicitacaoRepository.GetByIdAsync(id, cancellationToken);
                if (solicitacao == null)
                {
                    return null;
                }

                if (solicitacao.QuantidadeEnvios > 0 ||
                    solicitacao.Status == StatusSolicitacaoGravacao.Enviada)
                {
                    throw new InvalidOperationException(
                        "Uma gravação já foi despachada; aguarde a confirmação ou use a recuperação por retentativa.");
                }

                if (solicitacao.CartaoAnteriorId != null)
                {
                    var cartaoAnterior = await _cartaoRepository.GetByIdAsync(solicitacao.CartaoAnteriorId);
                    if (cartaoAnterior == null ||
                        cartaoAnterior.Status != StatusCartao.Bloqueado ||
                        cartaoAnterior.HistoricoAssociacaoCartao?.DataDesassociacao != null)
                    {
                        throw new InvalidOperationException(
                            "O cartão anterior não está no estado esperado para restauração explícita.");
                    }

                    var agora = DateTime.UtcNow;
                    cartaoAnterior.AlterarStatus(StatusCartao.Ativo);
                    var motivoRestauracao = $"Reemissão cancelada antes do envio: {motivo.Trim()}";
                    await _cartaoRepository.UpdateStatusWithHistoryAsync(
                        cartaoAnterior,
                        new HistoricoStatusCartao
                        {
                            CartaoId = cartaoAnterior.Id,
                            Status = StatusCartao.Ativo,
                            DataHora = agora,
                            Motivo = TruncarMotivo(motivoRestauracao),
                            AlteradoPorFuncionarioId = funcionarioId
                        });
                }

                if (!solicitacao.Cancelar(motivo.Trim(), funcionarioId, DateTime.UtcNow))
                {
                    throw new InvalidOperationException(
                        "A solicitação concluída ou cancelada não pode ser cancelada novamente.");
                }

                await _solicitacaoRepository.SaveChangesAsync(cancellationToken);
                var usuario = await _usuarioRepository.GetByIdAsync(solicitacao.UsuarioId);
                return await MapSolicitacaoAsync(solicitacao, usuario?.Nome);
            });
        }

        private async Task<string> GerarCredencialUnicaAsync(CancellationToken cancellationToken)
        {
            for (var tentativa = 0; tentativa < 10; tentativa++)
            {
                var credencial = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
                if (!await _solicitacaoRepository.CredentialExistsAsync(credencial, cancellationToken) &&
                    await _cartaoRepository.GetByUidAsync(credencial) == null)
                {
                    return credencial;
                }
            }

            throw new InvalidOperationException(
                "Não foi possível gerar uma credencial RFID única após várias tentativas.");
        }

        private static string TruncarMotivo(string motivo) =>
            motivo.Length <= 200 ? motivo : motivo[..200];

        private static GravacaoCartaoCommandDto? CriarComando(SolicitacaoGravacaoCartao solicitacao)
        {
            return solicitacao.Status is StatusSolicitacaoGravacao.Concluida or StatusSolicitacaoGravacao.Cancelada
                ? null
                : new GravacaoCartaoCommandDto(solicitacao.Id, solicitacao.Credencial);
        }

        private static Task<GravacaoCartaoResponseDto> MapSolicitacaoAsync(
            SolicitacaoGravacaoCartao solicitacao,
            string? nomeUsuario)
        {
            var dataSolicitacao = DateTime.SpecifyKind(solicitacao.DataSolicitacao, DateTimeKind.Utc);
            return Task.FromResult(new GravacaoCartaoResponseDto(
                solicitacao.Id,
                solicitacao.Status.ToString(),
                solicitacao.MotivoFalha ?? "Solicitação de gravação registrada.",
                solicitacao.UsuarioId,
                nomeUsuario ?? "Usuário não encontrado",
                new DateTimeOffset(dataSolicitacao),
                solicitacao.CartaoCriadoId));
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

                return cartoes.Select(cartao => MapToDto(cartao));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cartões");
                throw;
            }
        }

        public async Task<IEnumerable<CartaoDto>> GetByUsuarioIdAsync(string usuarioId)
        {
            _logger.LogInformation("Buscando cartões por UsuarioId: {UsuarioId}", usuarioId);

            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                {
                    throw new ArgumentException("ID do usuário é obrigatório", nameof(usuarioId));
                }

                var cartoes = await _cartaoRepository.GetByUsuarioIdAsync(usuarioId);

                _logger.LogInformation("Encontrados {Count} cartões ativos para o usuário {UsuarioId}", 
                    cartoes.Count(), usuarioId);

                return cartoes.Select(cartao => MapToDto(cartao));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cartões por UsuarioId: {UsuarioId}", usuarioId);
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

                if (string.IsNullOrWhiteSpace(updateCartaoDto.UsuarioId))
                {
                    throw new ArgumentException("ID do usuário é obrigatório", nameof(updateCartaoDto.UsuarioId));
                }

                var cartaoExistente = await _cartaoRepository.GetByIdAsync(id);
                if (cartaoExistente == null)
                {
                    throw new KeyNotFoundException($"Cartão com ID {id} não encontrado");
                }

                if (cartaoExistente.Uid != updateCartaoDto.Uid.Trim() ||
                    cartaoExistente.UsuarioId != updateCartaoDto.UsuarioId.Trim())
                {
                    throw new InvalidOperationException(
                        "UID e titular são imutáveis nesta emissão. Para trocar o titular, cancele o cartão e crie uma nova emissão.");
                }

                return MapToDto(cartaoExistente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cartão. ID: {Id}, UID: {Uid}", id, updateCartaoDto.Uid);
                throw;
            }
        }

        public Task<bool> BlockAsync(string id, string funcionarioId, string motivo) =>
            ChangeStatusAsync(id, StatusCartao.Bloqueado, funcionarioId, motivo);

        public Task<bool> UnblockAsync(string id, string funcionarioId, string motivo) =>
            ChangeStatusAsync(id, StatusCartao.Ativo, funcionarioId, motivo);

        public Task<bool> CancelAsync(string id, string funcionarioId, string motivo) =>
            ChangeStatusAsync(id, StatusCartao.Cancelado, funcionarioId, motivo);

        private async Task<bool> ChangeStatusAsync(
            string id,
            StatusCartao novoStatus,
            string funcionarioId,
            string motivo)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID é obrigatório.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(funcionarioId))
            {
                throw new ArgumentException("Funcionário responsável é obrigatório.", nameof(funcionarioId));
            }

            motivo = motivo?.Trim() ?? string.Empty;
            if (motivo.Length is 0 or > 200)
            {
                throw new ArgumentException("O motivo é obrigatório e deve ter até 200 caracteres.", nameof(motivo));
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var cartao = await _cartaoRepository.GetByIdAsync(id);
                if (cartao == null)
                {
                    return false;
                }

                if (cartao.Status == novoStatus)
                {
                    return true;
                }

                if (novoStatus == StatusCartao.Ativo)
                {
                    if (cartao.Status != StatusCartao.Bloqueado)
                    {
                        throw new InvalidOperationException("Somente um cartão bloqueado pode ser ativado.");
                    }

                    if (cartao.Usuario == null || !cartao.Usuario.IsActive)
                    {
                        throw new InvalidOperationException(
                            "O cartão não pode ser ativado enquanto seu titular estiver inativo.");
                    }

                    if ((await _cartaoRepository.GetByUsuarioIdAsync(cartao.UsuarioId))
                        .Any(cartaoAtivo => cartaoAtivo.Id != cartao.Id))
                    {
                        throw new InvalidOperationException(
                            "O titular já possui outro cartão ativo.");
                    }
                }

                cartao.AlterarStatus(novoStatus);
                var agora = DateTime.UtcNow;

                if (novoStatus == StatusCartao.Cancelado &&
                    cartao.HistoricoAssociacaoCartao is { DataDesassociacao: null } associacao)
                {
                    associacao.DataDesassociacao = agora;
                    associacao.DesassociadoPorFuncionarioId = funcionarioId;
                }

                await _cartaoRepository.UpdateStatusWithHistoryAsync(
                    cartao,
                    new HistoricoStatusCartao
                    {
                        CartaoId = cartao.Id,
                        Status = novoStatus,
                        DataHora = agora,
                        Motivo = motivo,
                        AlteradoPorFuncionarioId = funcionarioId
                    });

                return true;
            });
        }

        private static CartaoDto MapToDto(Cartao cartao, string? nomeUsuario = null)
        {
            return new CartaoDto
            {
                Id = cartao.Id,
                Uid = cartao.Uid,
                Status = cartao.Status,
                IsActive = cartao.Status == StatusCartao.Ativo,
                UsuarioId = cartao.UsuarioId,
                NomeUsuario = nomeUsuario ?? cartao.Usuario?.Nome ?? "Usuário não encontrado",
                DataExpiracao = cartao.DataExpiracao
            };
        }
    }
}