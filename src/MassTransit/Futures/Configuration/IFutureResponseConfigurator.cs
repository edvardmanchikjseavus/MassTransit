namespace MassTransit.Futures
{
    public interface IFutureResponseConfigurator<TResult, out TResponse> :
        IFutureResultConfigurator<TResult, TResponse>
        where TResult : class
        where TResponse : class
    {
        /// <summary>
        /// If specified, the identifier is used to complete a pending result and the result will be stored
        /// in the future.
        /// </summary>
        /// <param name="provider">Provides the identifier from the request</param>
        void CompletePendingRequest(PendingIdProvider<TResponse> provider);
    }
}
