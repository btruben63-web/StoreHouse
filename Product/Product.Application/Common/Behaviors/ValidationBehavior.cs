using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Product.Application.Common.Results;

namespace Product.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IReadOnlyCollection<IValidator<TRequest>> ?? validators.ToList();
        if (validatorList.Count == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();
        foreach (var validator in validatorList)
        {
            var validationResult = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(validationResult.Errors);
        }

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var errors = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return TResponse.Failure(Error.Validation(errors));
    }
}
