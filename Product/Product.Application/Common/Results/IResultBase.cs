namespace Product.Application.Common.Results;

public interface IResultBase<TSelf> where TSelf : IResultBase<TSelf>
{
    bool IsSuccess { get; }

    Error? Error { get; }

    static abstract TSelf Failure(Error error);
}
