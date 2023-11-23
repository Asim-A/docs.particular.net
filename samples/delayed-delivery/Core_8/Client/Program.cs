using System;
using System.Configuration;
using System.Threading.Tasks;
using NHibernate.Dialect;
using NServiceBus;
using NServiceBus.Persistence;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.DelayedDelivery.Client";
        var endpointConfiguration = new EndpointConfiguration("Samples.DelayedDelivery.Client");
        var hibernateConfig = new NHibernate.Cfg.Configuration();

        hibernateConfig.DataBaseIntegration(x =>
        {
            x.ConnectionStringName = "NServiceBus/Persistence";
            x.Dialect<MsSql2012Dialect>();
        });

        hibernateConfig.SetProperty("default_schema", "dbo");

        endpointConfiguration.UsePersistence<NHibernatePersistence>()
            .UseConfiguration(hibernateConfig); ;

        string errorQueue = "error";

        endpointConfiguration.SendFailedMessagesTo(errorQueue);
        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

        var messageStore = new SqlServerDelayedMessageStore(
                    connectionString: ConfigurationManager.ConnectionStrings["NServiceBus/Persistence"].ConnectionString,
                    schema: "dbo", tableName: "DelayMessageStoreTemp"); //optional, defaults to endpoint name with '.delayed' suffix

        var transport = new MsmqTransport
        {
            DelayedDelivery = new DelayedDeliverySettings(messageStore)

            {
                NumberOfRetries = 1,
                MaximumRecoveryFailuresPerSecond = 2,
                TimeToTriggerStoreCircuitBreaker = TimeSpan.FromSeconds(30),
                TimeToTriggerDispatchCircuitBreaker = TimeSpan.FromSeconds(30),
                TimeToTriggerFetchCircuitBreaker = TimeSpan.FromSeconds(30)
            }
        };

        var routing = endpointConfiguration.UseTransport(transport);

        endpointConfiguration.EnableInstallers();

        var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        await SendOrder(endpointInstance)
            .ConfigureAwait(false);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }

    static async Task SendOrder(IEndpointInstance endpointInstance)
    {
        Console.WriteLine("Press '1' to send PlaceOrder - defer message handling");
        Console.WriteLine("Press '2' to send PlaceDelayedOrder - defer message delivery");
        Console.WriteLine("Press any other key to exit");

        while (true)
        {
            var key = Console.ReadKey();
            Console.WriteLine();
            var id = Guid.NewGuid();

            switch (key.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    #region SendOrder
                    var placeOrder = new PlaceOrder
                    {
                        Product = "New shoes",
                        Id = id
                    };
                    await endpointInstance.Send("Samples.DelayedDelivery.Server", placeOrder)
                        .ConfigureAwait(false);
                    Console.WriteLine($"[Defer Message Handling] Sent a PlaceOrder message with id: {id.ToString("N")}");
                    #endregion
                    continue;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    #region DeferOrder
                    var placeDelayedOrder = new PlaceDelayedOrder
                    {
                        Product = "New shoes",
                        Id = id
                    };
                    var options = new SendOptions();

                    options.SetDestination("Samples.DelayedDelivery.Server");
                    options.DelayDeliveryWith(TimeSpan.FromSeconds(5));
                    await endpointInstance.Send(placeDelayedOrder, options)
                        .ConfigureAwait(false);
                    Console.WriteLine($"[Defer Message Delivery] Deferred a PlaceDelayedOrder message with id: {id.ToString("N")}");
                    #endregion
                    continue;
                default:
                    return;
            }
        }

    }
}
