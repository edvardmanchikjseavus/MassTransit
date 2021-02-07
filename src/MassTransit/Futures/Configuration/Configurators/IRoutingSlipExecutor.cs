namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Threading.Tasks;
    using Courier;


    public interface IRoutingSlipExecutor<in TInput>
        where TInput : class
    {
        Task Execute(FutureConsumeContext<TInput> context);

        bool Pending { set; }
    }


    public delegate Task BuildItineraryCallback<in TInput>(FutureConsumeContext<TInput> context, ItineraryBuilder builder)
        where TInput : class;
}
