namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Automatonymous;
    using Courier.Contracts;
    using GreenPipes;
    using Internals;
    using Metadata;


    public class FutureRoutingSlip<TRequest, TResponse, TFault, TInput> :
        BaseFutureConfigurator<TRequest, TResponse, TFault, RoutingSlipCompleted, RoutingSlipFaulted>,
        IFutureRoutingSlip<TInput>,
        IFutureRoutingSlipConfigurator<TResponse, TFault, TInput>
        where TRequest : class
        where TResponse : class
        where TInput : class
        where TFault : class
    {
        IRoutingSlipExecutor<TInput> _executor;

        public FutureRoutingSlip()
        {
            ResultIdProvider = GetTrackingNumber;

            _executor = new PlanRoutingSlipExecutor<TInput>();

            Fault(fault => fault.Init(context =>
            {
                var message = context.Instance.GetRequest<TRequest>();

                return new
                {
                    FaultId = context.MessageId ?? NewId.NextGuid(),
                    FaultedMessageId = context.Message.TrackingNumber,
                    FaultMessageTypes = TypeMetadataCache<TRequest>.MessageTypeNames,
                    Host = context.Message.ActivityExceptions.Select(x => x.Host).FirstOrDefault() ?? context.Host,
                    context.Message.Timestamp,
                    Exceptions = context.Message.ActivityExceptions.Select(x => x.ExceptionInfo).ToArray(),
                    Message = message
                };
            }));
        }

        public Task Execute(BehaviorContext<FutureState, TInput> context)
        {
            FutureConsumeContext<TInput> consumeContext = context.CreateFutureConsumeContext();

            return _executor.Execute(consumeContext);
        }

        public void Pending()
        {
            _executor.Pending = true;
        }

        public void Build(BuildItineraryCallback<TInput> buildItinerary)
        {
            _executor = new BuildRoutingSlipExecutor<TInput>(buildItinerary);
        }

        public void Plan()
        {
        }

        static Guid GetTrackingNumber(RoutingSlipCompleted message)
        {
            return message.TrackingNumber;
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            foreach (var result in base.Validate())
                yield return result;

            if (_response == null)
                yield return this.Failure("Response", "must be configured");

            if (_executor == null)
                yield return this.Failure("Build", "No itinerary builder, Build or Plan must be specified");
        }
    }
}
