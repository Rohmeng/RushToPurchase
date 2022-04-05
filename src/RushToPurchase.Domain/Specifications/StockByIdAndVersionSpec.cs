using Ardalis.Specification;
using RushToPurchase.Domain.Entities;

namespace RushToPurchase.Domain.Specifications;

public sealed class StockByIdAndVersionSpec : Specification<Stock>
{
    public StockByIdAndVersionSpec(int id, int version)
    {
        Query.Where(h => h.Id == id && h.Version == version);
    }
}