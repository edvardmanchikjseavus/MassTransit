namespace MassTransit.Futures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Automatonymous;
    using Configurators;
    using Contracts;
    using Courier.Contracts;
    using Definition;
    using Endpoints;
    using GreenPipes.Internals.Extensions;
    using Internals;
    using MassTransit.Configurators;

    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable MemberCanBeProtected.Global
    // ReSharper disable UnusedAutoPropertyAccessor.Global

    public abstract class Future<TRequest, TResponse, TFault> :
        MassTransitStateMachine<FutureState>
        where TRequest : class
        where TResponse : class
        where TFault : class
    {
        readonly FutureFault<TFault> _fault = new FutureFault<TFault>();
        readonly FutureResponse<TRequest, TResponse> _response = new FutureResponse<TRequest, TResponse>();

        protected Future()
        {
            InstanceState(x => x.CurrentState, WaitingForCompletion, Completed, Faulted);

            Event(() => FutureRequested, x => x.CorrelateById(context => CorrelationIdOrFault(context)));

            Event(() => ResponseRequested, e =>
            {
                e.CorrelateById(x => x.Message.CorrelationId);

                e.OnMissingInstance(x => x.Execute(context => throw new FutureNotFoundException(typeof(TRequest), context.Message.CorrelationId)));

                e.ConfigureConsumeTopology = false;
            });

            Initially(
                When(FutureRequested)
                    .InitializeFuture()
                    .TransitionTo(WaitingForCompletion)
            );

            During(WaitingForCompletion,
                When(FutureRequested)
                    .AddSubscription(),
                When(ResponseRequested)
                    .AddSubscription()
            );

            During(Completed,
                When(FutureRequested)
                    .RespondAsync(x => GetCompleted(x)),
                When(ResponseRequested)
                    .RespondAsync(x => GetCompleted(x))
            );

            During(Faulted,
                When(FutureRequested)
                    .RespondAsync(x => GetFaulted(x)),
                When(ResponseRequested)
                    .RespondAsync(x => GetFaulted(x))
            );

            Fault(x => x.Init(context =>
            {
                var message = context.Instance.GetRequest<TRequest>();

                // use supported message types to deserialize results...

                List<Fault> faults = context.Instance.Faults.Select(fault => fault.Value.ToObject<Fault>()).ToList();

                var faulted = faults.First();

                ExceptionInfo[] exceptions = faults.SelectMany(fault => fault.Exceptions).ToArray();

                return new
                {
                    faulted.FaultId,
                    faulted.FaultedMessageId,
                    Timestamp = context.Instance.Faulted,
                    Exceptions = exceptions,
                    faulted.Host,
                    faulted.FaultMessageTypes,
                    Message = message
                };
            }));
        }

        public State WaitingForCompletion { get; protected set; }
        public State Completed { get; protected set; }
        public State Faulted { get; protected set; }

        public Event<TRequest> FutureRequested { get; protected set; }
        public Event<Get<TRequest>> ResponseRequested { get; protected set; }

        /// <summary>
        /// Send a request when the future is requested
        /// </summary>
        /// <param name="configure"></param>
        /// <typeparam name="TCommand">The request type to send</typeparam>
        /// <typeparam name="TResult">The response type for the request</typeparam>
        protected void SendRequest<TCommand, TResult>(Action<IFutureRequestConfigurator<TResponse, TFault, TRequest, TCommand, TResult>> configure)
            where TCommand : class
            where TResult : class
        {
            IFutureRequest<TRequest> request = CreateRequest<TCommand, TResult>(configure);

            Initially(
                When(FutureRequested)
                    .ThenAsync(context => request.Send(context))
            );
        }

        /// <summary>
        /// Send a request when the future is requested, using a request property as the source
        /// </summary>
        /// <param name="inputSelector"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TCommand">The request type to send</typeparam>
        /// <typeparam name="TResult">The response type for the request</typeparam>
        /// <typeparam name="TInput">The input property type</typeparam>
        protected void SendRequest<TInput, TCommand, TResult>(Func<TRequest, TInput> inputSelector,
            Action<IFutureRequestConfigurator<TResponse, TFault, TInput, TCommand, TResult>> configure)
            where TInput : class
            where TCommand : class
            where TResult : class
        {
            IFutureRequest<TRequest, TInput> request = CreateRequest(configure);

            Initially(
                When(FutureRequested)
                    .ThenAsync(context => request.Send(context, inputSelector(context.Data)))
            );
        }

        /// <summary>
        /// Sends multiple requests when the future is requested, using an enumerable request property as the source
        /// </summary>
        /// <param name="inputSelector"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TCommand">The request type to send</typeparam>
        /// <typeparam name="TResult">The response type for the request</typeparam>
        /// <typeparam name="TInput">The input property type</typeparam>
        protected void SendRequests<TInput, TCommand, TResult>(Func<TRequest, IEnumerable<TInput>> inputSelector,
            Action<IFutureRequestConfigurator<TResponse, TFault, TInput, TCommand, TResult>> configure)
            where TInput : class
            where TCommand : class
            where TResult : class
        {
            IFutureRequest<TRequest, TInput> futureRequest = CreateRequest(configure);

            Initially(
                When(FutureRequested)
                    .ThenAsync(context => futureRequest.Send(context, inputSelector(context.Data)))
            );
        }

        /// <summary>
        /// Create a request, which can be used to send requests
        /// </summary>
        /// <param name="configure"></param>
        /// <typeparam name="TCommand">The request type to send</typeparam>
        /// <typeparam name="TResult">The response type for the request</typeparam>
        /// <returns></returns>
        protected IFutureRequest<TRequest> CreateRequest<TCommand, TResult>(
            Action<IFutureRequestConfigurator<TResponse, TFault, TRequest, TCommand, TResult>> configure)
            where TCommand : class
            where TResult : class
        {
            return CreateFutureRequest(configure);
        }

        /// <summary>
        /// Create a request, which can be used to send requests
        /// </summary>
        /// <param name="configure"></param>
        /// <typeparam name="TCommand">The request type to send</typeparam>
        /// <typeparam name="TResult">The response type for the request</typeparam>
        /// <typeparam name="TInput">The input property type</typeparam>
        /// <returns></returns>
        protected IFutureRequest<TRequest, TInput> CreateRequest<TInput, TCommand, TResult>(
            Action<IFutureRequestConfigurator<TResponse, TFault, TInput, TCommand, TResult>> configure)
            where TInput : class
            where TCommand : class
            where TResult : class
        {
            return CreateFutureRequest(configure);
        }

        /// <summary>
        /// Execute a routing slip when the future is requested
        /// </summary>
        /// <param name="configure"></param>
        protected void ExecuteRoutingSlip(Action<IFutureRoutingSlipConfigurator<TResponse, TFault, TRequest>> configure)
        {
            IFutureRoutingSlip<TRequest> request = CreateRoutingSlip(configure);

            Initially(
                When(FutureRequested)
                    .ThenAsync(context => request.Execute(context))
            );
        }

        /// <summary>
        /// Create a routing slip executor
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        protected IFutureRoutingSlip<TRequest> CreateRoutingSlip(Action<IFutureRoutingSlipConfigurator<TResponse, TFault, TRequest>> configure)
        {
            return CreateFutureRoutingSlip(configure);
        }

        /// <summary>
        /// Create a routing slip executor
        /// </summary>
        /// <param name="configure"></param>
        /// <typeparam name="TInput">The input property type</typeparam>
        /// <returns></returns>
        protected IFutureRoutingSlip<TInput> CreateRoutingSlip<TInput>(Action<IFutureRoutingSlipConfigurator<TResponse, TFault, TInput>> configure)
            where TInput : class
        {
            return CreateFutureRoutingSlip(configure);
        }

        FutureRequest<TRequest, TResponse, TFault, TInput, TCommand, TResult> CreateFutureRequest<TInput, TCommand, TResult>(
            Action<IFutureRequestConfigurator<TResponse, TFault, TInput, TCommand, TResult>> configure)
            where TCommand : class
            where TResult : class
            where TInput : class
        {
            var futureRequest = new FutureRequest<TRequest, TResponse, TFault, TInput, TCommand, TResult>();

            configure?.Invoke(futureRequest);

            BusConfigurationResult.CompileResults(futureRequest.Validate(), $"The future was not configured correctly: {TypeCache.GetShortName(GetType())}");

            if (futureRequest.HasResponse(out FutureResponse<TRequest, TResult, TResponse> response))
            {
                var eventName = FormatEventName<TResult>();

                Event<TResult> requestCompleted = Event<TResult>(eventName, x =>
                {
                    x.CorrelateById(m => RequestIdOrFault(m));

                    x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
                    x.ConfigureConsumeTopology = false;
                });

                DuringAny(
                    When(requestCompleted)
                        .ThenAsync(context => response.SetCompleted(context))
                        .TransitionTo(Completed)
                );

                eventName = FormatEventName<TCommand>();

                Event<Fault<TCommand>> requestFaulted = Event<Fault<TCommand>>(eventName + "Faulted", x =>
                {
                    x.CorrelateById(m => RequestIdOrFault(m));

                    x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
                    x.ConfigureConsumeTopology = false;
                });

                if (futureRequest.HasFault(out FutureFault<TRequest, TFault, Fault<TCommand>> fault))
                {
                    DuringAny(
                        When(requestFaulted)
                            .ThenAsync(context => fault.SetFaulted(context))
                            .TransitionTo(Faulted)
                    );
                }
                else
                {
                    DuringAny(
                        When(requestFaulted)
                            .ThenAsync(context => _fault.SetFaulted(context))
                            .TransitionTo(Faulted)
                    );
                }
            }
            else
            {
                CompleteEvent(futureRequest.ResultIdProvider);
                FaultEvent(futureRequest.CommandIdProvider);
            }

            return futureRequest;
        }

        FutureRoutingSlip<TRequest, TResponse, TFault, TInput> CreateFutureRoutingSlip<TInput>(
            Action<IFutureRoutingSlipConfigurator<TResponse, TFault, TInput>> configure)
            where TInput : class
        {
            var routingSlip = new FutureRoutingSlip<TRequest, TResponse, TFault, TInput>();

            configure?.Invoke(routingSlip);

            BusConfigurationResult.CompileResults(routingSlip.Validate(), $"The future was not configured correctly: {TypeCache.GetShortName(GetType())}");

            var eventName = FormatEventName<RoutingSlipCompleted>();

            Event<RoutingSlipCompleted> requestCompleted = Event<RoutingSlipCompleted>(eventName, x =>
            {
                x.CorrelateById(m => FutureIdOrFault(m.Message.Variables));

                x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), FutureIdOrDefault(context.Message.Variables))));
                x.ConfigureConsumeTopology = false;
            });

            eventName = FormatEventName<RoutingSlipFaulted>();

            Event<RoutingSlipFaulted> requestFaulted = Event<RoutingSlipFaulted>(eventName, x =>
            {
                x.CorrelateById(m => FutureIdOrFault(m.Message.Variables));

                x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), FutureIdOrDefault(context.Message.Variables))));
                x.ConfigureConsumeTopology = false;
            });

            if (routingSlip.HasResponse(out FutureResponse<TRequest, RoutingSlipCompleted, TResponse> response))
            {
                DuringAny(
                    When(requestCompleted)
                        .ThenAsync(context => response.SetCompleted(context))
                        .TransitionTo(Completed)
                );

                if (routingSlip.HasFault(out FutureFault<TRequest, TFault, RoutingSlipFaulted> fault))
                {
                    DuringAny(
                        When(requestFaulted)
                            .ThenAsync(context => fault.SetFaulted(context))
                            .TransitionTo(Faulted)
                    );
                }
                else
                {
                    DuringAny(
                        When(requestFaulted)
                            .ThenAsync(context => _fault.SetFaulted(context))
                            .TransitionTo(Faulted)
                    );
                }
            }
            else
            {
                DuringAny(
                    When(requestCompleted)
                        .SetResult(x => routingSlip.ResultIdProvider(x.Message), x => x.Message)
                        .IfElse(context => context.Instance.Completed.HasValue,
                            completed => completed
                                .ThenAsync(context => _response.SetCompleted(context))
                                .TransitionTo(Completed),
                            notCompleted => notCompleted.If(context => context.Instance.Faulted.HasValue,
                                faulted => faulted
                                    .ThenAsync(context => _fault.SetFaulted(context))
                                    .TransitionTo(Faulted))),
                When(requestFaulted)
                    .SetFault(x => x.Message)
                    .If(context => context.Instance.Faulted.HasValue,
                        faulted => faulted
                            .ThenAsync(context => _fault.SetFaulted(context))
                            .TransitionTo(Faulted))
                );
            }

            return routingSlip;
        }

        protected void CompleteEvent<T>(PendingIdProvider<T> pendingIdProvider)
            where T : class
        {
            var eventName = FormatEventName<T>();

            Event<T> requestCompleted = Event<T>(eventName, x =>
            {
                x.CorrelateById(m => RequestIdOrFault(m));

                x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
                x.ConfigureConsumeTopology = false;
            });

            DuringAny(
                When(requestCompleted)
                    .SetResult(x => pendingIdProvider(x.Message), x => x.Message)
                    .IfElse(context => context.Instance.Completed.HasValue,
                        completed => completed
                            .ThenAsync(context => _response.SetCompleted(context))
                            .TransitionTo(Completed),
                        notCompleted => notCompleted.If(context => context.Instance.Faulted.HasValue,
                            faulted => faulted
                                .ThenAsync(context => _fault.SetFaulted(context))
                                .TransitionTo(Faulted)))
            );
        }

        protected void FaultEvent<T>(PendingIdProvider<T> pendingIdProvider)
            where T : class
        {
            var eventName = FormatEventName<T>() + "Faulted";

            Event<Fault<T>> requestFaulted = Event<Fault<T>>(eventName, x =>
            {
                x.CorrelateById(m => RequestIdOrFault(m));

                x.OnMissingInstance(m => m.Execute(context => throw new FutureNotFoundException(GetType(), RequestIdOrDefault(context))));
                x.ConfigureConsumeTopology = false;
            });

            DuringAny(
                When(requestFaulted)
                    .SetFault(x => pendingIdProvider(x.Message.Message), x => x.Message)
                    .If(context => context.Instance.Faulted.HasValue,
                        faulted => faulted
                            .ThenAsync(context => _fault.SetFaulted(context))
                            .TransitionTo(Faulted))
            );
        }

        static string FormatEventName<T>()
            where T : class
        {
            return DefaultEndpointNameFormatter.Instance.Message<T>();
        }

        protected static Guid RequestIdOrFault(MessageContext context)
        {
            return context.RequestId ?? throw new RequestException("RequestId not present, but required");
        }

        protected static Guid RequestIdOrDefault(MessageContext context)
        {
            return context.RequestId ?? default;
        }

        protected static Guid FutureIdOrFault(IDictionary<string, object> variables)
        {
            if (variables.TryGetValue(nameof(FutureConsumeContext.FutureId), out Guid correlationId))
                return correlationId;

            throw new RequestException("CorrelationId not present, define the routing slip using Event");
        }

        protected static Guid FutureIdOrDefault(IDictionary<string, object> variables)
        {
            return variables.TryGetValue(nameof(FutureConsumeContext.FutureId), out Guid correlationId) ? correlationId : default;
        }

        protected static Guid CorrelationIdOrFault(MessageContext context)
        {
            return context.CorrelationId ?? throw new RequestException("CorrelationId not present, define the request correlation using Event");
        }

        protected void Response(Action<IFutureResponseConfigurator<TResponse>> configure)
        {
            var configurator = new FutureResponseConfigurator<TRequest, TResponse>(_response);

            configure?.Invoke(configurator);
        }

        protected void Fault(Action<IFutureFaultConfigurator<TFault>> configure)
        {
            var configurator = new FutureFaultConfigurator<TFault>(_fault);

            configure?.Invoke(configurator);
        }

        protected Task<TResponse> GetCompleted(EventContext<FutureState> context)
        {
            if (context.Instance.TryGetResult(context.Instance.CorrelationId, out TResponse completed))
                return Task.FromResult(completed);

            throw new InvalidOperationException("Completed result not available");
        }

        protected Task<TFault> GetFaulted(EventContext<FutureState> context)
        {
            if (context.Instance.TryGetFault(context.Instance.CorrelationId, out TFault faulted))
                return Task.FromResult(faulted);

            throw new InvalidOperationException("Faulted result not available");
        }
    }


    /*
     * *- CorrelationId - identifies the future
    - Location - where to communicate with the future
    - Type - the completed future type
    - FaultType - the faulted future type
     */


    public abstract class Future<TRequest, TResponse> :
        Future<TRequest, TResponse, Fault<TRequest>>
        where TRequest : class
        where TResponse : class
    {
    }
}
