using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs;

public sealed record AlterarStatusCartaoDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Motivo { get; init; }
}
