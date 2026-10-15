using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class AuditTransactionTests
{
    [Fact]
    public async Task FailedCardAuditRollsBackTheStatusChange()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        string cardId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var user = new Usuario
            {
                Id = "user",
                Nome = "Titular",
                Documento = "document",
                IsActive = true
            };
            var employee = new Funcionario
            {
                Id = "operator",
                UsuarioId = user.Id,
                Usuario = user,
                SenhaHash = "hash",
                Departamento = "Ops",
                Cargo = "Operator",
                IsActive = true
            };
            var card = new Cartao
            {
                Uid = "0123456789ABCDEF",
                UsuarioId = user.Id
            };
            cardId = card.Id;
            setupContext.AddRange(user, employee, card);
            await setupContext.SaveChangesAsync();
        }

        await using (var operationContext = new AppDbContext(options))
        {
            var repository = new PersistThenFailCartaoRepository(operationContext);
            var userRepository = new Mock<IUsuarioRepository>();
            var employeeRepository = new Mock<IFuncionarioRepository>();
            var requestRepository = new Mock<ISolicitacaoGravacaoCartaoRepository>();
            var unitOfWork = new UnitOfWork(operationContext, NullLogger<UnitOfWork>.Instance);
            var service = new CartaoService(
                repository,
                userRepository.Object,
                employeeRepository.Object,
                requestRepository.Object,
                unitOfWork,
                NullLogger<CartaoService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.BlockAsync(cardId, "operator", "Audit insert failed"));
        }

        await using var verificationContext = new AppDbContext(options);
        var persistedCard = await verificationContext.Cartoes.SingleAsync(card => card.Id == cardId);
        Assert.Equal(StatusCartao.Ativo, persistedCard.Status);
        Assert.Empty(await verificationContext.HistoricoStatusCartao.ToListAsync());
    }

    private sealed class PersistThenFailCartaoRepository(AppDbContext context) : ICartaoRepository
    {
        public Task<Cartao?> GetByIdAsync(string id) => context.Cartoes.SingleOrDefaultAsync(card => card.Id == id);

        public async Task<Cartao> UpdateStatusWithHistoryAsync(
            Cartao card,
            HistoricoStatusCartao history)
        {
            context.Cartoes.Update(card);
            context.HistoricoStatusCartao.Add(history);
            await context.SaveChangesAsync();
            throw new InvalidOperationException("Simulated failure after saving audit data.");
        }

        public Task<Cartao> CreateWithAuditAsync(
            Cartao card,
            HistoricoAssociacoesCartao association,
            HistoricoStatusCartao history) => throw new NotSupportedException();

        public Task<Cartao?> GetByUidAsync(string uid) => throw new NotSupportedException();
        public Task<IEnumerable<Cartao>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<Cartao>> GetByUsuarioIdAsync(string usuarioId) => throw new NotSupportedException();
        public Task<IEnumerable<Cartao>> GetAllByUsuarioIdAsync(string usuarioId) => throw new NotSupportedException();
        public Task<bool> UidExistsAsync(string uid) => throw new NotSupportedException();
    }
}
