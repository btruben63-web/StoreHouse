namespace Product.Application.Common.Results;

public sealed class Result : IResultBase<Result>
{
    private Result(Error? error) => Error = error;

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : IResultBase<Result<T>>
{
    private readonly T? _value;

    private Result(T value) => _value = value;

    private Result(Error error) => Error = error;

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
