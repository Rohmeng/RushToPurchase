using FreeSql.DataAnnotations;
using RushToPurchase.Domain.SharedKernel.Interfaces;
using RushToPurchase.Domain.SharedKernel;

namespace RushToPurchase.Domain.Entities;

public class Stock : Entity, IAggregateRoot
{
    [Column(Name = "id", DbType = "int unsigned", IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 库存
    /// </summary>
    [Column(Name = "count", DbType = "int")]
    public int Count { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [Column(Name = "name", StringLength = 50, IsNullable = false)]
    public string Name { get; set; }

    /// <summary>
    /// 已售
    /// </summary>
    [Column(Name = "sale", DbType = "int")]
    public int Sale { get; set; }

    /// <summary>
    /// 乐观锁，版本号
    /// </summary>
    [Column(Name = "version", DbType = "int")]
    public int Version { get; set; }

}
