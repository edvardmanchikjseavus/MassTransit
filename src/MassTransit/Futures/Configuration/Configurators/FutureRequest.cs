namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Automatonymous;
    using Endpoints;
    using GreenPipes;
    using Internals;
    using Util;


    public class FutureRequest<TRequest, TResponse, TFault, TInput, TCommand, TResult> :
        BaseFutureConfigurator<TRequest, TResponse, TFault, TResult, Fault<TCommand>>,
        IFutureRequest<TRequest, TInput>,
        IFutureRequest<TInput>,
        IFutureRequestConfigurator<TResponse, TFault, TInput, TCommand, TResult>
        where TRequest : class
        where TResponse : class
        where TFault : class
        where TInput : class
        where TCommand : class
        where TResult : class
    {
        readonly FutureCommand<TInput, TCommand> _command = new FutureCommand<TInput, TCommand>();

        public PendingIdProvider<TCommand> CommandIdProvider { get; private set; }

        public Task Send(BehaviorContext<FutureState, TInput> context)
        {
            FutureConsumeContext<TInput> consumeContext = context.CreateFutureConsumeContext();

            return _command.SendCommand(consumeContext);
        }

        public Task Send(BehaviorContext<FutureState, TRequest> context, TInput input)
        {
            if (input == null)
                return TaskUtil.Completed;

            FutureConsumeContext<TInput> consumeContext = context.CreateFutureConsumeContext(input);

            return _command.SendCommand(consumeContext);
        }

        public Task Send(BehaviorContext<FutureState, TRequest> context, IEnumerable<TInput> inputs)
        {
            return inputs != null
                ? Task.WhenAll(inputs.Select(input => Send(context, input)))
                : TaskUtil.Completed;
        }

        public void Pending(PendingIdProvider<TCommand> commandIdProvider, PendingIdProvider<TResult> resultIdProvider)
        {
            CommandIdProvider = commandIdProvider;
            ResultIdProvider = resultIdProvider;

            _command.PendingIdProvider = CommandIdProvider;
        }

        public void Command(Action<IFutureCommandConfigurator<TInput, TCommand>> configure)
        {
            var configurator = new FutureCommandConfigurator<TInput, TCommand>(_command);

            configure?.Invoke(configurator);
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            foreach (var result in base.Validate())
                yield return result;

            foreach (var result in _command.Validate())
                yield return result;

            if (_response == null)
            {
                if (CommandIdProvider == null)
                    yield return this.Failure("SelectCommandId", "Must be configured");
            }
        }
    }
}
