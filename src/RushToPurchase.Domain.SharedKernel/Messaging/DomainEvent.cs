namespace RushToPurchase.Domain.SharedKernel.Messaging;

public abstract class DomainEvent : Event
{
    protected DomainEvent(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }
}