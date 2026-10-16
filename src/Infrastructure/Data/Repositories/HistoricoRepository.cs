using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ControleDeAcesso.Infrastructure.Data.Repositories
{
    public class HistoricoRepository : IHistoricoRepository
    {
        private readonly AppDbContext _context;

        public HistoricoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetStatusUsuarioAsync(string usuarioId, int page, int pageSize)
        {
            if (!await _context.Usuarios.AnyAsync(u => u.Id == usuarioId))
            {
                return null;
            }

            var query = _context.HistoricoStatusUsuario
                .AsNoTracking()
                .Where(h => h.UsuarioId == usuarioId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(h => h.DataHora).ThenByDescending(h => h.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(h => new HistoricoStatusAtivoDto
                {
                    Id = h.Id,
                    IsActive = h.IsActive,
                    DataHora = h.DataHora,
                    Motivo = h.Motivo,
                    AlteradoPorFuncionarioId = h.AlteradoPorFuncionarioId,
                    AlteradoPorNome = h.AlteradoPorFuncionario != null ? h.AlteradoPorFuncionario.Usuario.Nome : null
                })
                .ToListAsync();

            return new PagedResultDto<HistoricoStatusAtivoDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
        }

        public async Task<PagedResultDto<HistoricoStatusAtivoDto>?> GetStatusFuncionarioAsync(string funcionarioId, int page, int pageSize)
        {
            if (!await _context.Funcionarios.AnyAsync(f => f.Id == funcionarioId))
            {
                return null;
            }

            var query = _context.HistoricoStatusFuncionario
                .AsNoTracking()
                .Where(h => h.FuncionarioId == funcionarioId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(h => h.DataHora).ThenByDescending(h => h.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(h => new HistoricoStatusAtivoDto
                {
                    Id = h.Id,
                    IsActive = h.IsActive,
                    DataHora = h.DataHora,
                    Motivo = h.Motivo,
                    AlteradoPorFuncionarioId = h.AlteradoPorFuncionarioId,
                    AlteradoPorNome = h.AlteradoPorFuncionario != null ? h.AlteradoPorFuncionario.Usuario.Nome : null
                })
                .ToListAsync();

            return new PagedResultDto<HistoricoStatusAtivoDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
        }

        public async Task<HistoricoCartaoDto?> GetCartaoAsync(string cartaoId, int page, int pageSize)
        {
            var cartao = await _context.Cartoes
                .AsNoTracking()
                .Where(c => c.Id == cartaoId)
                .Select(c => new { c.Id, c.Uid })
                .FirstOrDefaultAsync();

            if (cartao == null)
            {
                return null;
            }

            var associacao = await _context.HistoricoAssociacoesCartao
                .AsNoTracking()
                .Where(h => h.CartaoId == cartaoId)
                .Select(h => new HistoricoAssociacaoCartaoDto
                {
                    Id = h.Id,
                    DataAssociacao = h.DataAssociacao,
                    DataDesassociacao = h.DataDesassociacao,
                    AssociadoPorFuncionarioId = h.AssociadoPorFuncionarioId,
                    AssociadoPorNome = h.AssociadoPorFuncionario != null ? h.AssociadoPorFuncionario.Usuario.Nome : null,
                    DesassociadoPorFuncionarioId = h.DesassociadoPorFuncionarioId,
                    DesassociadoPorNome = h.DesassociadoPorFuncionario != null ? h.DesassociadoPorFuncionario.Usuario.Nome : null
                })
                .FirstOrDefaultAsync();

            var query = _context.HistoricoStatusCartao
                .AsNoTracking()
                .Where(h => h.CartaoId == cartaoId);

            var total = await query.CountAsync();
            var statusRows = await query
                .OrderByDescending(h => h.DataHora).ThenByDescending(h => h.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(h => new HistoricoStatusCartaoDto
                {
                    Id = h.Id,
                    Status = h.Status,
                    DataHora = h.DataHora,
                    Motivo = h.Motivo,
                    AlteradoPorFuncionarioId = h.AlteradoPorFuncionarioId,
                    AlteradoPorNome = h.AlteradoPorFuncionario != null ? h.AlteradoPorFuncionario.Usuario.Nome : null
                })
                .ToListAsync();

            foreach (var row in statusRows)
            {
                row.StatusNome = row.Status.ToString();
            }

            return new HistoricoCartaoDto
            {
                CartaoId = cartao.Id,
                Uid = cartao.Uid,
                Associacao = associacao,
                Status = new PagedResultDto<HistoricoStatusCartaoDto>
                {
                    Items = statusRows,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = total
                }
            };
        }
    }
}
