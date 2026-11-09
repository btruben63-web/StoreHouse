using Microsoft.EntityFrameworkCore;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;

namespace Product.Infrastructure.Persistence;

internal sealed class UnitOfWork(ProductDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The entity was modified by another operation.", exception);
        }
    }

    public Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
        where TResult : IResultBase<TResult>
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(token);

            var result = await operation(token);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(token);
            }
            else
            {
                await transaction.RollbackAsync(token);
            }

            return result;
        }, cancellationToken);
    }
}
