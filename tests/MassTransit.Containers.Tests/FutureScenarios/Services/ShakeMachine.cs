namespace MassTransit.Containers.Tests.FutureScenarios.Services
{
    using System.Threading.Tasks;
    using Contracts;


    public class ShakeMachine :
        IShakeMachine
    {
        public async Task PourShake(string flavor, Size size)
        {
        }
    }
}
