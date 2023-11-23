using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class PlaceOrderHandler :
    IHandleMessages<PlaceOrder>,
    IHandleMessages<PlaceOrderNew>
{
    static ILog log = LogManager.GetLogger<PlaceOrderHandler>();
    static List<Guid> wasMessageDelayed = new List<Guid>();

    public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
    {
        if (ShouldMessageBeDelayed(message.Id))
        {
            var options = new SendOptions();

            options.DelayDeliveryWith(TimeSpan.FromSeconds(5));

            options.RouteToThisEndpoint();

            await context.Send(new PlaceOrderNew() { Id = message.Id, Product = message.Product }, options)

                .ConfigureAwait(false);

            log.Info($"[Defer Message Handling] Deferring Message with Id: {message.Id}");

            return;
        }

        log.Info($"[Defer Message Handling] Order for Product:{message.Product} placed with id: {message.Id}");
    }

    public Task Handle(PlaceOrderNew message, IMessageHandlerContext context)

    {
        log.Info($"[Defer Message Handling] New Mewssagsldkfjsdl Message with Id: {message.Id}");

        return Task.CompletedTask;
    }

    bool ShouldMessageBeDelayed(Guid id)
    {
        if (wasMessageDelayed.Contains(id))
        {
            return false;
        }

        wasMessageDelayed.Add(id);
        return true;
    }
}
