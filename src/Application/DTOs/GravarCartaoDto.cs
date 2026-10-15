using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>Dados para emitir um cartão novo ou reemitir uma credencial existente.</summary>
    public sealed record GravarCartaoDto
    {
        [Required]
        [StringLength(36)]
        public required string UsuarioId { get; init; }

        [StringLength(36)]
        public string? CartaoAnteriorId { get; init; }

        [StringLength(200)]
        public string? MotivoReemissao { get; init; }
    }
}
