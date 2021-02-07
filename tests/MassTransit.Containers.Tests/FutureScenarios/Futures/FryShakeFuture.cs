namespace MassTransit.Containers.Tests.FutureScenarios.Futures
{
    using Contracts;
    using MassTransit.Futures;


    public class FryShakeFuture :
        Future<OrderFryShake, FryShakeCompleted>
    {
        public FryShakeFuture()
        {
            Event(() => FutureRequested, x => x.CorrelateById(context => context.Message.OrderLineId));

            SendRequest<OrderFry, FryCompleted>(x =>
            {
                x.Pending(m => m.OrderLineId, m => m.OrderLineId);
                x.Command(c =>
                {
                    c.Init(context => new
                    {
                        OrderId = context.Instance.CorrelationId,
                        OrderLineId = InVar.Id,
                        context.Message.Size,
                    });
                });
            });

            SendRequest<OrderShake, ShakeCompleted>(x =>
            {
                x.Pending(m => m.OrderLineId, m => m.OrderLineId);

                x.Command(c =>
                {
                    c.Init(context => new
                    {
                        OrderId = context.Instance.CorrelationId,
                        OrderLineId = InVar.Id,
                        context.Message.Flavor,
                        context.Message.Size,
                    });
                });
            });

            Response(x => x.Init(context =>
            {
                var message = context.Instance.GetRequest<OrderFryShake>();

                return new {Description = $"{message.Size} {message.Flavor} FryShake({context.Instance.Results.Count})"};
            }));
        }
    }
}
