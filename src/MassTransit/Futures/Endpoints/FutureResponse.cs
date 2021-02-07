namespace MassTransit.Futures.Endpoints
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Automatonymous;
    using GreenPipes;
    using Internals;


    public class FutureResponse<TRequest, TResult, TResponse> :
        ISpecification
        where TRequest : class
        where TResult : class
        where TResponse : class
    {
        IResponseEndpoint<TResult> _endpoint;

        public AsyncFutureMessageFactory<TResult, TResponse> Factory
        {
            set => _endpoint = new FactoryResponseEndpoint<TResult, TResponse>(value);
        }

        public InitializerValueProvider<TResult> Initializer
        {
            set => _endpoint = new InitializerResponseEndpoint<TRequest, TResult, TResponse>(value);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_endpoint == null)
                yield return this.Failure("Response", "Factory", "Init or Create must be configured");
        }

        public Task SetCompleted(BehaviorContext<FutureState, TResult> context)
        {
            FutureConsumeContext<TResult> consumeContext = context.CreateFutureConsumeContext();

            return SendResponse(consumeContext);
        }

        public Task SendResponse(FutureConsumeContext<TResult> context)
        {
            return context.Instance.HasSubscriptions()
                ? _endpoint.SendResponse(context, context.Instance.Subscriptions.ToArray())
                : _endpoint.SendResponse(context);
        }
    }


    public class FutureResponse<TRequest, TResponse> :
        ISpecification
        where TRequest : class
        where TResponse : class
    {
        IResponseEndpoint _endpoint;

        public AsyncFutureMessageFactory<TResponse> Factory
        {
            set => _endpoint = new FactoryResponseEndpoint<TResponse>(value);
        }

        public InitializerValueProvider Initializer
        {
            set => _endpoint = new InitializerResponseEndpoint<TRequest, TResponse>(value);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_endpoint == null)
                yield return this.Failure("Response", "Factory", "Init or Create must be configured");
        }

        public Task SetCompleted(BehaviorContext<FutureState> context)
        {
            var consumeContext = context.CreateFutureConsumeContext();

            return SendResponse(consumeContext, consumeContext.Instance.Subscriptions.ToArray());
        }

        public Task SendResponse(FutureConsumeContext context, params FutureSubscription[] subscriptions)
        {
            return context.Instance.HasSubscriptions()
                ? _endpoint.SendResponse(context, subscriptions)
                : _endpoint.SendResponse(context);
        }
    }
}
