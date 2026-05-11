using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Domain.Common;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<SupplierQuote> SupplierQuotes => Set<SupplierQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserWarehouse>(e =>
        {
            e.ToTable("UserWarehouses");
            e.HasIndex(x => new { x.UserId, x.WarehouseId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserWarehouses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Warehouse).WithMany(w => w.UserWarehouses).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasOne(x => x.ParentCategory).WithMany(c => c.ChildCategories).HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasIndex(x => x.Sku).IsUnique();
            e.HasIndex(x => x.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(e => { e.ToTable("Warehouses"); });

        modelBuilder.Entity<Location>(e =>
        {
            e.ToTable("Locations");
            e.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
            e.Property(x => x.MaxCapacity);
            e.Property(x => x.PickSortOrder);
            e.HasOne(x => x.Warehouse).WithMany(w => w.Locations).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.MovementsFrom).WithOne(m => m.FromLocation).HasForeignKey(m => m.FromLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.MovementsTo).WithOne(m => m.ToLocation).HasForeignKey(m => m.ToLocationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBalance>(e =>
        {
            e.ToTable("StockBalances");
            e.HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();
            e.HasOne(x => x.Product).WithMany(p => p.StockBalances).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Location).WithMany(l => l.StockBalances).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovements");
            e.Property(x => x.MovementType).HasConversion<int>();
            e.HasOne(x => x.Product).WithMany(p => p.StockMovements).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany(u => u.StockMovements).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Supplier>(e => { e.ToTable("Suppliers"); });

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.CompanyName).HasMaxLength(256);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.TaxNumber).HasMaxLength(64);
            e.Property(x => x.City).HasMaxLength(128);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Email);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.Supplier).WithMany(s => s.Orders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("OrderItems");
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(p => p.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.Property(x => x.Type).HasConversion<int>();
            e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequisition>(e =>
        {
            e.ToTable("PurchaseRequisitions");
            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.Warehouse).WithMany(w => w.PurchaseRequisitions).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RequestedByUser).WithMany(u => u.PurchaseRequisitionsRequested).HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedByUser).WithMany(u => u.PurchaseRequisitionsApproved).HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseRequisitionLine>(e =>
        {
            e.ToTable("PurchaseRequisitionLines");
            e.HasOne(x => x.PurchaseRequisition).WithMany(r => r.Lines).HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(p => p.PurchaseRequisitionLines).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierQuote>(e =>
        {
            e.ToTable("SupplierQuotes");
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Currency).HasMaxLength(8);
            e.HasOne(x => x.PurchaseRequisition).WithMany(r => r.SupplierQuotes).HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Supplier).WithMany(s => s.SupplierQuotes).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var uid = _currentUser?.UserId;
        var utc = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedDate == default)
                        entry.Entity.CreatedDate = utc;
                    entry.Entity.CreatedBy = uid;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = utc;
                    entry.Entity.UpdatedBy = uid;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Notification>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedDate == default)
                entry.Entity.CreatedDate = utc;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
