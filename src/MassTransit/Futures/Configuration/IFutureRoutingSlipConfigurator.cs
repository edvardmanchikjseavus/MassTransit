namespace MassTransit.Futures
{
    using System;
    using Configurators;
    using Courier.Contracts;


    public interface IFutureRoutingSlipConfigurator<TResponse, TFault, out TInput>
        where TResponse : class
        where TFault : class
        where TInput : class
    {
        void Pending();

        void Build(BuildItineraryCallback<TInput> buildItinerary);

        void Plan();

        void Response(Action<IFutureResponseConfigurator<RoutingSlipCompleted, TResponse>> configure);

        void Fault(Action<IFutureFaultConfigurator<TFault, RoutingSlipFaulted>> configure);
    }
}
