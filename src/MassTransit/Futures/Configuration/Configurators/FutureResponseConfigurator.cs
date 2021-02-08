namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Automatonymous;
    using Endpoints;
    using GreenPipes;


    public class FutureResponseConfigurator<TCommand, TResult, TFault, TRequest, TResponse> :
        FutureResponseHandle<TCommand, TResult, TFault, TRequest, TResponse>,
        IFutureResponseConfigurator<TResult, TResponse>,
        ISpecification
        where TCommand : class
        where TResult : class
        where TFault : class
        where TRequest : class
        where TResponse : class
    {
        readonly FutureRequestHandle<TCommand, TResult, TFault, TRequest> _request;
        FutureResult<TCommand, TResponse, TResult> _result;

        public FutureResponseConfigurator(Event<TResponse> completed, FutureRequestHandle<TCommand, TResult, TFault, TRequest> request)
        {
            _request = request;

            Completed = completed;
        }

        public PendingIdProvider<TResponse> PendingResponseIdProvider { get; private set; }

        public Event<TResponse> Completed { get; }

        public Event<Fault<TRequest>> Faulted => _request.Faulted;

        public FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(
            Action<IFutureResponseConfigurator<TResult, T>> configure = default)
            where T : class
        {
            return _request.OnResponseReceived(configure);
        }

        public void CompletePendingRequest(PendingIdProvider<TResponse> provider)
        {
            PendingResponseIdProvider = provider;
        }

        public void SetCompletedUsingFactory(FutureMessageFactory<TResponse, TResult> factoryMethod)
        {
            GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
        }

        public void SetCompletedUsingFactory(AsyncFutureMessageFactory<TResponse, TResult> factoryMethod)
        {
            GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
        }

        public void SetCompletedUsingInitializer(InitializerValueProvider<TResponse> valueProvider)
        {
            GetResultConfigurator().SetCompletedUsingInitializer(valueProvider);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        public Task SetResult(BehaviorContext<FutureState, TResponse> context)
        {
            return _result.SetResult(context);
        }

        IFutureResultConfigurator<TResult, TResponse> GetResultConfigurator()
        {
            _result ??= new FutureResult<TCommand, TResponse, TResult>();

            var configurator = new FutureResultConfigurator<TCommand, TResult, TResponse>(_result);
            return configurator;
        }
    }
}
