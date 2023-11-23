
using System;
using NServiceBus;
#region PlaceOrder
public class PlaceOrder :
    ICommand
{
    public Guid Id { get; set; }
    public string Product { get; set; }
}

#endregion

public class PlaceOrderNew : ICommand
{

    public Guid Id { get; set; }

    public string Product { get; set; }

}