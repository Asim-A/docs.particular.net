using Versioning.Contracts;
using NServiceBus;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.Versioning.V1.Publisher";
        var endpointConfiguration = new EndpointConfiguration("Samples.Versioning.V1.Publisher");
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());

        var endpointInstance = await Endpoint.Start(endpointConfiguration);

        Console.WriteLine("Press enter to publish a message");
        Console.WriteLine("Press any key to exit");
        while (true)
        {
            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.Key != ConsoleKey.Enter)
            {
                break;
            }

            await endpointInstance.Publish<ISomethingHappened>(sh => { sh.SomeData = 1; });

            Console.WriteLine("Published event.");
        }

        await endpointInstance.Stop();
    }
}