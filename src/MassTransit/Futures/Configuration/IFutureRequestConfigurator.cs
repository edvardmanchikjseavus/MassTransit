namespace MassTransit.Futures
{
    using System;


    public interface IFutureRequestConfigurator<TResponse, TFault, out TInput, TCommand, out TResult>
        where TResponse : class
        where TFault : class
        where TInput : class
        where TCommand : class
        where TResult : class
    {
        void Pending(PendingIdProvider<TCommand> commandIdProvider, PendingIdProvider<TResult> resultIdProvider);

        void Command(Action<IFutureCommandConfigurator<TInput, TCommand>> configure);

        void Response(Action<IFutureResponseConfigurator<TResult, TResponse>> configure);

        void Fault(Action<IFutureFaultConfigurator<TFault, Fault<TCommand>>> configure);
    }
}
