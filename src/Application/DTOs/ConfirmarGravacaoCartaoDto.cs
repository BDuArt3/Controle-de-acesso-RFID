using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>Confirmação autenticada do Worker após gravar uma credencial no cartão.</summary>
    public sealed record ConfirmarGravacaoCartaoDto
    {
        [Required]
        [StringLength(36)]
        public required string SolicitacaoId { get; init; }

        [Required]
        [StringLength(16, MinimumLength = 16)]
        public required string CredencialGravada { get; init; }
    }

    /// <summary>Falha autenticada do Worker durante a gravação de um cartão.</summary>
    public sealed record FalharGravacaoCartaoDto
    {
        [Required]
        [StringLength(36)]
        public required string SolicitacaoId { get; init; }

        [Required]
        [StringLength(200)]
        public required string Motivo { get; init; }
    }

    /// <summary>Motivo obrigatório para cancelar uma solicitação de gravação.</summary>
    public sealed record CancelarGravacaoCartaoDto
    {
        [Required]
        [StringLength(200)]
        public required string Motivo { get; init; }
    }
}
