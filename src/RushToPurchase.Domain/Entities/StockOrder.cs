using FreeSql.DataAnnotations;
using RushToPurchase.Domain.SharedKernel;
using RushToPurchase.Domain.SharedKernel.Interfaces;

namespace RushToPurchase.Domain.Entities;

public class StockOrder: Entity, IAggregateRoot
{
    [Column(Name = "id", DbType = "int unsigned", IsPrimary = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(Name = "create_time", DbType = "timestamp", InsertValueSql = "CURRENT_TIMESTAMP")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    [Column(Name = "name", StringLength = 30, IsNullable = false)]
    public string Name { get; set; }

    /// <summary>
    /// 库存ID
    /// </summary>
    [Column(Name = "sid", DbType = "int")]
    public int Sid { get; set; }

    [Column(Name = "user_id", DbType = "int")]
    public int UserId { get; set; } = 0;
}
