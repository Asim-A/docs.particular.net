namespace Versioning.Contracts
{
    using NServiceBus;

    public interface ISomethingHappened :
        IEvent
    {
        int SomeData { get; set; }
    }
}