using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.FairDistribution.Client";
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVXYZ";
        var random = new Random();

        var endpointConfiguration = new EndpointConfiguration("Samples.FairDistribution.Client");
        endpointConfiguration.UsePersistence<NonDurablePersistence>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        AddRouting(endpointConfiguration);

        AddFairDistributionClient(endpointConfiguration);

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press enter to send a message");
        Console.WriteLine("Press any key to exit");

        while (true)
        {
            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.Key != ConsoleKey.Enter)
            {
                break;
            }
            var orderId = new string(Enumerable.Range(0, 4).Select(x => letters[random.Next(letters.Length)]).ToArray());
            Console.WriteLine($"Placing order {orderId}");
            var message = new PlaceOrder
            {
                OrderId = orderId,
                Value = random.Next(100)
            };
            await endpointInstance.Send(message)
                .ConfigureAwait(false);
        }

        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }

    static void AddFairDistributionClient(EndpointConfiguration endpointConfiguration)
    {
        #region FairDistributionClient

        endpointConfiguration.EnableFeature<FairDistribution>();
        var routing = endpointConfiguration.UseTransport(new MsmqTransport());
        var settings = endpointConfiguration.GetSettings();
        var strategy = new FairDistributionStrategy(
            settings: settings,
            endpoint: "Samples.FairDistribution.Server",
            scope: DistributionStrategyScope.Send);
        routing.SetMessageDistributionStrategy(strategy);

        #endregion
    }

    static void AddRouting(EndpointConfiguration endpointConfiguration)
    {
        var routing = endpointConfiguration.UseTransport(new MsmqTransport());

        #region Routing

        routing.RouteToEndpoint(typeof(PlaceOrder), "Samples.FairDistribution.Server");

        #endregion
    }
}
