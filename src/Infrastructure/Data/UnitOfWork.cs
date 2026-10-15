using System.Runtime.ExceptionServices;
using System.Data;
using ControleDeAcesso.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControleDeAcesso.Infrastructure.Data
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var result = await operation();
                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception operationException)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackException)
                    {
                        _logger.LogError(rollbackException, "Falha ao reverter a transação após erro na operação.");
                    }

                    _context.ChangeTracker.Clear();
                    ExceptionDispatchInfo.Capture(operationException).Throw();
                    throw;
                }
            });
        }
    }
}
