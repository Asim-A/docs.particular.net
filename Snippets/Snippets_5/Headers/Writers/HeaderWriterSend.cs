﻿using System.Threading;
using NServiceBus;
using NServiceBus.MessageMutator;
using NUnit.Framework;
using Operations.Msmq;

[TestFixture]
public class HeaderWriterSend
{
    public static ManualResetEvent ManualResetEvent;

    string endpointName = "HeaderWriterSendV5";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        QueueCreation.DeleteQueuesForEndpoint(endpointName);
    }

    [Test]
    public void Write()
    {
        ManualResetEvent = new ManualResetEvent(false);

        BusConfiguration busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName(endpointName);
        busConfiguration.TypesToScan(TypeScanner.TypesFor<HeaderWriterSend>());
        busConfiguration.EnableInstallers();
        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.RegisterComponents(c => c.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall));
        using (IStartableBus startableBus = Bus.Create(busConfiguration))
        using (IBus bus = startableBus.Start())
        {
            bus.SendLocal(new MessageToSend());
            ManualResetEvent.WaitOne();
        }
    }

    class MessageToSend : IMessage
    {
    }
    
    class MessageHandler : IHandleMessages<MessageToSend>
    {
        public void Handle(MessageToSend message)
        {
        }
    }

    class Mutator : IMutateIncomingTransportMessages
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
            string headerText = HeaderWriter.ToFriendlyString<HeaderWriterSend>(transportMessage.Headers);
            SnippetLogger.Write(headerText);
            ManualResetEvent.Set();
        }
    }
}