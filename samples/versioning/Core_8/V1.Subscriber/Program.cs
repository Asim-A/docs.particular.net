using System;
using System.Threading.Tasks;
using NServiceBus;
using Versioning.Contracts;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.Versioning.V1.Subscriber";
        var endpointConfiguration = new EndpointConfiguration("Samples.Versioning.V1.Subscriber");
        endpointConfiguration.UsePersistence<NonDurablePersistence>();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        var routing = endpointConfiguration.UseTransport(new MsmqTransport());
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableInstallers();

        #region V1SubscriberMapping

        routing.RegisterPublisher(
            assembly: typeof(ISomethingHappened).Assembly,
            publisherEndpoint: "Samples.Versioning.V1Publisher");

        #endregion

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}
