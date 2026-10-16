using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs;

public sealed record AlterarStatusFuncionarioDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Motivo { get; init; }
}
