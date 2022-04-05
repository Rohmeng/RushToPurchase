using RushToPurchase.Domain.SharedKernel.Messaging;

namespace RushToPurchase.Domain.SharedKernel.Events;

public interface IEventStore
{
    void Save<T>(T theEvent) where T : Event;
}