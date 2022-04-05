using Ardalis.Specification;

namespace RushToPurchase.Domain.SharedKernel.Interfaces;

public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot
{
    int ExecuteSqlRaw(string sql, params object[] parameters);
}