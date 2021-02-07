namespace MassTransit.Containers.Tests.FutureScenarios.Contracts
{
    public interface FryCompleted :
        OrderLineCompleted
    {
        Size Size { get; }
    }
}