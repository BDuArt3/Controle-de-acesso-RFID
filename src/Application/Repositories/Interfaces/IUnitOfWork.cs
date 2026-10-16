namespace ControleDeAcesso.Application.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    }
}
