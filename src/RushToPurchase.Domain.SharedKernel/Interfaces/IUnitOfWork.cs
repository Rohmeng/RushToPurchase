namespace RushToPurchase.Domain.SharedKernel.Interfaces;

public interface IUnitOfWork
{
    Task<bool> Commit();
}