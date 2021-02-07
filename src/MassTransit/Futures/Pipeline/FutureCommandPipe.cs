namespace MassTransit.Futures.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using GreenPipes;
    using Util;


    class FutureCommandPipe<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly Guid _requestId;
        readonly Uri _responseAddress;

        public FutureCommandPipe(Uri responseAddress, Guid requestId)
        {
            _responseAddress = responseAddress;
            _requestId = requestId;
        }

        public Task Send(SendContext<T> context)
        {
            context.ResponseAddress = _responseAddress;
            context.RequestId = _requestId;

            return TaskUtil.Completed;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope(nameof(FutureResponsePipe<T>));
        }
    }
}
