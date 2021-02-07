namespace MassTransit.Futures.Endpoints
{
    using System.Threading.Tasks;
    using Automatonymous;
    using Pipeline;


    public class FactoryCommandEndpoint<TRequest, TCommand> :
        ICommandEndpoint<TRequest, TCommand>
        where TRequest : class
        where TCommand : class
    {
        readonly AsyncFutureMessageFactory<TRequest, TCommand> _factory;
        DestinationAddressProvider<FutureState> _destinationAddressProvider;
        PendingIdProvider<TCommand> _pendingIdProvider;

        public FactoryCommandEndpoint(DestinationAddressProvider<FutureState> destinationAddressProvider, PendingIdProvider<TCommand> pendingIdProvider,
            AsyncFutureMessageFactory<TRequest, TCommand> factory)
        {
            _destinationAddressProvider = destinationAddressProvider;
            _pendingIdProvider = pendingIdProvider;
            _factory = factory;
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
            var command = await _factory(context).ConfigureAwait(false);

            var destinationAddress = _destinationAddressProvider(context);

            var pipe = new FutureCommandPipe<TCommand>(context.ReceiveContext.InputAddress, context.Instance.CorrelationId);

            if (destinationAddress != null)
            {
                var endpoint = await context.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

                await endpoint.Send(command, pipe, context.CancellationToken).ConfigureAwait(false);
            }
            else
                await context.Publish(command, pipe, context.CancellationToken).ConfigureAwait(false);

            if (_pendingIdProvider != null)
            {
                var pendingId = _pendingIdProvider(command);
                context.Instance.Pending.Add(pendingId);
            }
        }
    }
}
