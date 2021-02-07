namespace MassTransit.Futures.Endpoints
{
    using System.Threading.Tasks;
    using Automatonymous;


    public interface ICommandEndpoint<in TRequest, out TCommand>
        where TRequest : class
        where TCommand : class
    {
        DestinationAddressProvider<FutureState> DestinationAddressProvider { set; }

        PendingIdProvider<TCommand> PendingIdProvider { set; }

        Task SendCommand(FutureConsumeContext<TRequest> context);
    }
}
