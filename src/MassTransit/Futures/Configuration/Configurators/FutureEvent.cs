namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Collections.Generic;
    using Automatonymous;
    using GreenPipes;


    public class FutureEvent<TCommand, TResult> :
        IFutureEventConfigurator<TCommand, TResult>,
        ISpecification
        where TCommand : class
        where TResult : class
    {
        public FutureEvent(Event<TResult> requestCompleted, Event<Fault<TCommand>> requestFaulted)
        {
            Completed = requestCompleted;
            Faulted = requestFaulted;
        }

        public Event<TResult> Completed { get; }
        public Event<Fault<TCommand>> Faulted { get; }

        public Func<TCommand, Guid> CommandIdSelector { get; private set; }
        public Func<TResult, Guid> ResultIdSelector { get; private set; }

        public void SelectResultId(Func<TResult, Guid> selector)
        {
            ResultIdSelector = selector;
        }

        public void SelectCommandId(Func<TCommand, Guid> selector)
        {
            CommandIdSelector = selector;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (ResultIdSelector == null)
                yield return this.Failure("SelectResultId", "Must be configured");
            if (CommandIdSelector == null)
                yield return this.Failure("SelectCommandId", "Must be configured");
        }
    }
}
