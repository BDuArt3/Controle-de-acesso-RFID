using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }

    public class HistoricoStatusAtivoDto
    {
        public long Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime DataHora { get; set; }
        public string? Motivo { get; set; }
        public string? AlteradoPorFuncionarioId { get; set; }
        public string? AlteradoPorNome { get; set; }
    }

    public class HistoricoStatusCartaoDto
    {
        public long Id { get; set; }
        public StatusCartao Status { get; set; }
        public string StatusNome { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public string? Motivo { get; set; }
        public string? AlteradoPorFuncionarioId { get; set; }
        public string? AlteradoPorNome { get; set; }
    }

    public class HistoricoAssociacaoCartaoDto
    {
        public long Id { get; set; }
        public DateTime DataAssociacao { get; set; }
        public DateTime? DataDesassociacao { get; set; }
        public string? AssociadoPorFuncionarioId { get; set; }
        public string? AssociadoPorNome { get; set; }
        public string? DesassociadoPorFuncionarioId { get; set; }
        public string? DesassociadoPorNome { get; set; }
    }

    public class HistoricoCartaoDto
    {
        public string CartaoId { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public HistoricoAssociacaoCartaoDto? Associacao { get; set; }
        public PagedResultDto<HistoricoStatusCartaoDto> Status { get; set; } = new();
    }
}
