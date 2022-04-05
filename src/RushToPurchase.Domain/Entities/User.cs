using FreeSql.DataAnnotations;
using RushToPurchase.Domain.SharedKernel;
using RushToPurchase.Domain.SharedKernel.Interfaces;

namespace RushToPurchase.Domain.Entities;

public class User: Entity, IAggregateRoot
{
    [Column(Name = "id", DbType = "bigint", IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    [Column(Name = "user_name", IsNullable = false)]
    public string UserName { get; set; }
}