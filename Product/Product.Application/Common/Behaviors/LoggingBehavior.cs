using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;

namespace Product.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.HandlingRequest(requestName);
        var startTimestamp = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        logger.HandledRequest(requestName, Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
        if (response.Error is { } error)
        {
            logger.RequestFailed(requestName, error.Code, error.Message);
        }

        return response;
    }
}
