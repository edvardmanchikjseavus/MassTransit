namespace MassTransit.Futures.Configurators
{
    using System;
    using System.Collections.Generic;
    using Endpoints;
    using GreenPipes;


    public abstract class BaseFutureConfigurator<TRequest, TResponse, TRequestFault, TResult, TFault> :
        ISpecification
        where TRequest : class
        where TRequestFault : class
        where TResponse : class
        where TResult : class
        where TFault : class
    {
        protected FutureFault<TRequest, TRequestFault, TFault> _fault;
        protected FutureResponse<TRequest, TResult, TResponse> _response;

        public PendingIdProvider<TResult> ResultIdProvider { get; protected set; }

        public virtual IEnumerable<ValidationResult> Validate()
        {
            if (_response != null)
            {
                foreach (var result in _response.Validate())
                    yield return result;
            }
            else
            {
                if (ResultIdProvider == null)
                    yield return this.Failure("SelectResultId", "Must be configured");
            }

            if (_fault != null)
            {
                foreach (var result in _fault.Validate())
                    yield return result;
            }
        }

        public void Response(Action<IFutureResponseConfigurator<TResult, TResponse>> configure)
        {
            _response ??= new FutureResponse<TRequest, TResult, TResponse>();

            var configurator = new FutureResponseConfigurator<TRequest, TResult, TResponse>(_response);

            configure?.Invoke(configurator);
        }

        public void Fault(Action<IFutureFaultConfigurator<TRequestFault, TFault>> configure)
        {
            _fault ??= new FutureFault<TRequest, TRequestFault, TFault>();

            var configurator = new FutureFaultConfigurator<TRequest, TRequestFault, TFault>(_fault);

            configure?.Invoke(configurator);
        }

        public bool HasResponse(out FutureResponse<TRequest, TResult, TResponse> response)
        {
            response = _response;
            return response != null;
        }

        public bool HasFault(out FutureFault<TRequest, TRequestFault, TFault> fault)
        {
            fault = _fault;
            return fault != null;
        }
    }
}
