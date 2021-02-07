namespace MassTransit.Futures.Endpoints
{
    using System.Threading.Tasks;
    using Automatonymous;
    using Context;
    using Initializers;
    using Pipeline;


    public class InitializerCommandEndpoint<TRequest, TCommand> :
        ICommandEndpoint<TRequest, TCommand>
        where TRequest : class
        where TCommand : class
    {
        readonly InitializerValueProvider<TRequest> _provider;
        DestinationAddressProvider<FutureState> _destinationAddressProvider;
        PendingIdProvider<TCommand> _pendingIdProvider;

        public InitializerCommandEndpoint(DestinationAddressProvider<FutureState> destinationAddressProvider, PendingIdProvider<TCommand> pendingIdProvider,
            InitializerValueProvider<TRequest> provider)
        {
            DestinationAddressProvider = destinationAddressProvider;
            _pendingIdProvider = pendingIdProvider;
            _provider = provider;
        }

        public DestinationAddressProvider<FutureState> DestinationAddressProvider
        {
            set => _destinationAddressProvider = value;
        }

        public PendingIdProvider<TCommand> PendingIdProvider
        {
            set => _pendingIdProvider = value;
        }

        public async Task SendCommand(FutureConsumeContext<TRequest> context)
        {
            InitializeContext<TCommand> initializeContext = await MessageInitializerCache<TCommand>.Initialize(context.Message, context.CancellationToken);

            var destinationAddress = _destinationAddressProvider(context);

            var endpoint = destinationAddress != null
                ? await context.GetSendEndpoint(destinationAddress).ConfigureAwait(false)
                : new ConsumeSendEndpoint(await context.ReceiveContext.PublishEndpointProvider.GetPublishSendEndpoint<TCommand>().ConfigureAwait(false),
                    context, default);

            var pipe = new FutureCommandPipe<TCommand>(context.ReceiveContext.InputAddress, context.Instance.CorrelationId);

            var values = _provider(context);

            IMessageInitializer<TCommand> messageInitializer = MessageInitializerCache<TCommand>.GetInitializer(values.GetType());

            var command = await messageInitializer.Send(endpoint, initializeContext, values, pipe).ConfigureAwait(false);

            if (_pendingIdProvider != null)
            {
                var pendingId = _pendingIdProvider(command);
                context.Instance.Pending.Add(pendingId);
            }
        }
    }
}
