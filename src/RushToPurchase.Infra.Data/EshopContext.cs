using Microsoft.EntityFrameworkCore;
using RushToPurchase.Domain.Entities;

namespace RushToPurchase.Infra.Data;

public partial class EshopContext : DbContext
{
    public EshopContext()
    {
    }

    public EshopContext(DbContextOptions<EshopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Stock> Stocks { get; set; }
    public virtual DbSet<StockOrder> StockOrders { get; set; }
    public virtual DbSet<User> Users { get; set; }

//         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//         {
//             if (!optionsBuilder.IsConfigured)
//             {
// #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//                 optionsBuilder.UseMySql("server=localhost;user id=root;password=qwer1234;port=3306;database=m4a_miaosha", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.28-mysql"));
//             }
//         }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.ToTable("stock");

            entity.HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Count)
                .HasColumnName("count")
                .HasComment("库存");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name")
                .HasDefaultValueSql("''")
                .HasComment("名称");

            entity.Property(e => e.Sale)
                .HasColumnName("sale")
                .HasComment("已售");

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasComment("乐观锁，版本号");
            // .IsRowVersion();

        });

        modelBuilder.Entity<StockOrder>(entity =>
        {
            entity.ToTable("stock_order");

            entity.HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.CreateTime)
                .HasColumnType("timestamp")
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnName("create_time")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("创建时间");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnName("name")
                .HasDefaultValueSql("''")
                .HasComment("商品名称");

            entity.Property(e => e.Sid)
                .HasColumnName("sid")
                .HasComment("库存ID");

            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_name")
                .HasDefaultValueSql("''");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}