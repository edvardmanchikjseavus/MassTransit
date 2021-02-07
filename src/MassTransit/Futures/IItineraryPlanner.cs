namespace MassTransit.Futures
{
    using System.Threading.Tasks;
    using Courier;


    public interface IItineraryPlanner<in T>
        where T : class
    {
        Task PlanItinerary(FutureConsumeContext<T> value, ItineraryBuilder builder);
    }
}
