using Ardalis.Specification;

namespace RushToPurchase.Domain.SharedKernel.Interfaces;

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot
{
}
