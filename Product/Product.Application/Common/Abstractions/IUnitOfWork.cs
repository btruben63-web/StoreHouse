using Product.Application.Common.Results;

namespace Product.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : IResultBase<TResult>;
}
