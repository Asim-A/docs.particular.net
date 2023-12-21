﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Shared;

namespace Receiver
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices(services => services.AddSingleton<IHostedService>(new ProceedIfRabbitMqIsAlive("rabbitmq")))
                .UseNServiceBus(ctx =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Samples.Docker.Receiver");
                    #region TransportConfiguration

                    var rabbitMqConnectionString = "host=rabbitmq";
                    var transport = endpointConfiguration.UseTransport(new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), rabbitMqConnectionString ));

                    #endregion

                    endpointConfiguration.UseSerialization<SystemJsonSerializer>();
                    endpointConfiguration.DefineCriticalErrorAction(CriticalErrorActions.RestartContainer);
                    endpointConfiguration.EnableInstallers();

                    return endpointConfiguration;
                });
        }
    }
}