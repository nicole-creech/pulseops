using Microsoft.EntityFrameworkCore;
using PulseOps.Domain.Entities;

namespace PulseOps.Infrastructure.Persistence;

public class PulseOpsDbContext : DbContext
{
    public PulseOpsDbContext(DbContextOptions<PulseOpsDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();

            entity.HasOne(x => x.Business)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.BusinessId);

            entity.HasIndex(x => x.Email);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Sku).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Price).HasColumnType("numeric(18,2)");

            entity.HasOne(x => x.Business)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BusinessId);

            entity.HasIndex(x => new { x.BusinessId, x.Sku }).IsUnique();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Product)
                .WithOne(x => x.InventoryItem)
                .HasForeignKey<InventoryItem>(x => x.ProductId);

            entity.HasIndex(x => x.ProductId).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(50);

            entity.HasOne(x => x.Business)
                .WithMany(x => x.Customers)
                .HasForeignKey(x => x.BusinessId);

            entity.HasIndex(x => new { x.BusinessId, x.Email });
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(x => x.Business)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.BusinessId);

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId);

            entity.HasIndex(x => new { x.BusinessId, x.OrderNumber }).IsUnique();
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(x => x.LineTotal).HasColumnType("numeric(18,2)");

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.ProductId);
        });
    }
}