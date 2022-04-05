using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RushToPurchase.Domain.SharedKernel.Interfaces;

namespace RushToPurchase.Infra.Data;


// inherit from Ardalis.Specification type
public class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
    private readonly EshopContext _dbContext;

    public EfRepository(EshopContext dbContext) : base(dbContext)
    {
        this._dbContext = dbContext;
    }
    
    // IUnitOfWork UnitOfWork { get; }
    public int ExecuteSqlRaw(string sql, params object[] parameters)
    {
        return _dbContext.Database.ExecuteSqlRaw(sql, parameters);
    }
}