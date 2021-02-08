namespace MassTransit.Futures
{
    using System;
    using System.Threading.Tasks;
    using Automatonymous;


    public interface IFutureStateMachineConfigurator
    {
        Event<T> CreateResponseEvent<T>()
            where T : class;

        /// <summary>
        /// Set the Future's result to the specified value
        /// </summary>
        /// <param name="responseReceived"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        void SetResult<T>(Event<T> responseReceived, Func<BehaviorContext<FutureState, T>, Task> callback)
            where T : class;

        /// <summary>
        /// Set the Future to the Faulted state, and set the Fault message
        /// </summary>
        /// <param name="requestCompleted"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        void SetFaulted<T>(Event<T> requestCompleted, Func<BehaviorContext<FutureState, T>, Task> callback)
            where T : class;

        /// <summary>
        /// Set the result for a pending request and remove the identifier
        /// </summary>
        /// <param name="requestCompleted"></param>
        /// <param name="pendingIdProvider"></param>
        /// <typeparam name="T"></typeparam>
        void CompletePendingRequest<T>(Event<T> requestCompleted, PendingIdProvider<T> pendingIdProvider)
            where T : class;
    }
}
