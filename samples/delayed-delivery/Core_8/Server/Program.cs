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
        Console.Title = "Samples.DelayedDelivery.Server";
        var endpointConfiguration = new EndpointConfiguration("Samples.DelayedDelivery.Server");
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
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}
