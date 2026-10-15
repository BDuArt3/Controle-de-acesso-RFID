using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface ICartaoService
    {
        Task<GravacaoSolicitadaResult> SolicitarGravacaoAsync(
            GravarCartaoDto request,
            string funcionarioId,
            string idempotencyKey,
            CancellationToken cancellationToken);
        Task<GravacaoCartaoResponseDto?> GetSolicitacaoAsync(string id, CancellationToken cancellationToken);
        Task<GravacaoCartaoCommandDto?> PrepararRetentativaAsync(string id, CancellationToken cancellationToken);
        Task<bool> RegistrarDespachoAsync(string id, CancellationToken cancellationToken);
        Task<GravacaoCartaoResponseDto?> ConfirmarGravacaoAsync(
            ConfirmarGravacaoCartaoDto request,
            CancellationToken cancellationToken);
        Task<GravacaoCartaoResponseDto?> RegistrarFalhaGravacaoAsync(
            FalharGravacaoCartaoDto request,
            CancellationToken cancellationToken);
        Task<GravacaoCartaoResponseDto?> CancelarSolicitacaoAsync(
            string id,
            string funcionarioId,
            string motivo,
            CancellationToken cancellationToken);
        Task<CartaoDto?> GetByIdAsync(string id);
        Task<CartaoDto?> GetByUidAsync(string uid);
        Task<IEnumerable<CartaoDto>> GetAllAsync();
        Task<IEnumerable<CartaoDto>> GetByUsuarioIdAsync(string usuarioId);
        Task<CartaoDto> UpdateAsync(string id, CreateCartaoDto updateCartaoDto);
        Task<bool> BlockAsync(string id, string funcionarioId, string motivo);
        Task<bool> UnblockAsync(string id, string funcionarioId, string motivo);
        Task<bool> CancelAsync(string id, string funcionarioId, string motivo);
    }
}