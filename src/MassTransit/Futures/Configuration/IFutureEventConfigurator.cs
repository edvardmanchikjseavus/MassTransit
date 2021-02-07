namespace MassTransit.Futures
{
    using System;


    public interface IFutureEventConfigurator<out TCommand, out TResult>
        where TCommand : class
        where TResult : class
    {
        void SelectResultId(Func<TResult, Guid> selector);
        void SelectCommandId(Func<TCommand, Guid> selector);
    }
}
