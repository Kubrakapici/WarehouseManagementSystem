using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);

        if (!applied.Any() && !pending.Any())
            await context.Database.EnsureCreatedAsync(cancellationToken);
        else
            await context.Database.MigrateAsync(cancellationToken);

        if (await context.Roles.AnyAsync(cancellationToken))
        {
            await EnsureViewerAsync(context, passwordHasher, cancellationToken);
            return;
        }

        var roles = new List<Role>
        {
            new() { Id = Guid.NewGuid(), Name = RoleNames.Admin, Description = "Tam sistem yetkisi" },
            new() { Id = Guid.NewGuid(), Name = RoleNames.WarehouseStaff, Description = "Depo operasyonları" },
            new() { Id = Guid.NewGuid(), Name = RoleNames.Operations, Description = "Operasyon ve sipariş" },
            new() { Id = Guid.NewGuid(), Name = RoleNames.Manager, Description = "Yönetim ve raporlama" },
            new() { Id = Guid.NewGuid(), Name = RoleNames.Viewer, Description = "Salt okunur izleyici" }
        };

        await context.Roles.AddRangeAsync(roles, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var adminRole = roles.First(r => r.Name == RoleNames.Admin);
        var staffRole = roles.First(r => r.Name == RoleNames.WarehouseStaff);
        var opsRole = roles.First(r => r.Name == RoleNames.Operations);
        var managerRole = roles.First(r => r.Name == RoleNames.Manager);
        var viewerRole = roles.First(r => r.Name == RoleNames.Viewer);

        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "admin@wms.local",
                PasswordHash = passwordHasher.HashPassword("Admin@123"),
                FirstName = "Sistem",
                LastName = "Admin",
                IsActive = true,
                RoleId = adminRole.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "depo@wms.local",
                PasswordHash = passwordHasher.HashPassword("Staff@123"),
                FirstName = "Depo",
                LastName = "Personeli",
                IsActive = true,
                RoleId = staffRole.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "operasyon@wms.local",
                PasswordHash = passwordHasher.HashPassword("Ops@123"),
                FirstName = "Operasyon",
                LastName = "Ekibi",
                IsActive = true,
                RoleId = opsRole.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "yonetici@wms.local",
                PasswordHash = passwordHasher.HashPassword("Manager@123"),
                FirstName = "Bolge",
                LastName = "Yöneticisi",
                IsActive = true,
                RoleId = managerRole.Id
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "izleyici@wms.local",
                PasswordHash = passwordHasher.HashPassword("Viewer@123"),
                FirstName = "Demo",
                LastName = "Izleyici",
                IsActive = true,
                RoleId = viewerRole.Id
            }
        };

        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var adminUser = users.First(u => u.Email == "admin@wms.local");

        var catElectronics = new Category { Id = Guid.NewGuid(), Name = "Elektronik", Description = "Genel elektronik" };
        var catComputers = new Category { Id = Guid.NewGuid(), Name = "Bilgisayar", ParentCategoryId = catElectronics.Id };
        var catPhones = new Category { Id = Guid.NewGuid(), Name = "Telefon", ParentCategoryId = catElectronics.Id };

        await context.Categories.AddRangeAsync(new[] { catElectronics, catComputers, catPhones }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Istanbul Merkez Depo", City = "Istanbul", Address = "Organize Sanayi Bolgesi", IsActive = true };
        await context.Warehouses.AddAsync(warehouse, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var locA = new Location
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Corridor = "A",
            Shelf = "01",
            Floor = "B-03",
            Code = "A-01-B-03"
        };
        var locB = new Location
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouse.Id,
            Corridor = "B",
            Shelf = "02",
            Floor = "01",
            Code = "B-02-01"
        };

        await context.Locations.AddRangeAsync(new[] { locA, locB }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var pLaptop = new Product
        {
            Id = Guid.NewGuid(),
            Name = "İş İstasyonu Laptop",
            Sku = "SKU-LPT-001",
            Barcode = "8680001112223",
            CategoryId = catComputers.Id,
            UnitPrice = 42500m,
            MinimumStockLevel = 5,
            IsActive = true,
            QrCodeData = "WMS:SKU-LPT-001"
        };

        var pPhone = new Product
        {
            Id = Guid.NewGuid(),
            Name = "El Terminali",
            Sku = "SKU-HND-010",
            Barcode = "8680004445556",
            CategoryId = catPhones.Id,
            UnitPrice = 8900m,
            MinimumStockLevel = 10,
            IsActive = true,
            QrCodeData = "WMS:SKU-HND-010"
        };

        await context.Products.AddRangeAsync(new[] { pLaptop, pPhone }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var balLaptopA = new StockBalance { Id = Guid.NewGuid(), ProductId = pLaptop.Id, LocationId = locA.Id, Quantity = 10 };
        var balPhoneB = new StockBalance { Id = Guid.NewGuid(), ProductId = pPhone.Id, LocationId = locB.Id, Quantity = 4 };

        await context.StockBalances.AddRangeAsync(new[] { balLaptopA, balPhoneB }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Teknik TED Ltd.",
            Phone = "+90 212 555 0101",
            Email = "siparis@tekniktedarik.local",
            Address = "Istanbul",
            IsActive = true
        };

        await context.Suppliers.AddAsync(supplier, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-1001",
            Status = OrderStatus.Pending,
            SupplierId = supplier.Id,
            Notes = "Acil sipariş",
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = pPhone.Id,
                    Quantity = 20,
                    UnitPrice = pPhone.UnitPrice
                }
            }
        };

        foreach (var item in order.Items)
            item.OrderId = order.Id;

        await context.Orders.AddAsync(order, cancellationToken);

        var now = DateTime.UtcNow;
        var movements = new List<StockMovement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MovementType = StockMovementType.Entry,
                Quantity = 12,
                Description = "Açılış stok",
                ProductId = pLaptop.Id,
                ToLocationId = locA.Id,
                UserId = adminUser.Id,
                CreatedDate = now.AddHours(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MovementType = StockMovementType.Exit,
                Quantity = 2,
                Description = "Gün içi çıkış",
                ProductId = pLaptop.Id,
                FromLocationId = locA.Id,
                UserId = adminUser.Id,
                CreatedDate = now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MovementType = StockMovementType.Entry,
                Quantity = 4,
                Description = "Mal kabul",
                ProductId = pPhone.Id,
                ToLocationId = locB.Id,
                UserId = adminUser.Id,
                CreatedDate = now.AddMinutes(-45)
            }
        };

        await context.StockMovements.AddRangeAsync(movements, cancellationToken);

        await context.Notifications.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = adminUser.Id,
            Title = "Sistem hazır",
            Message = "Depo yönetim sistemi başarıyla kuruldu.",
            Type = NotificationType.Success,
            IsRead = false,
            CreatedDate = now
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sistem zaten kurulu ise izleyici rolu/kullanicisi yoksa idempotent olarak ekler.
    /// </summary>
    private static async Task EnsureViewerAsync(AppDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        var viewerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Viewer, cancellationToken);
        if (viewerRole == null)
        {
            viewerRole = new Role { Id = Guid.NewGuid(), Name = RoleNames.Viewer, Description = "Salt okunur izleyici" };
            await context.Roles.AddAsync(viewerRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var hasViewerUser = await context.Users.AnyAsync(u => u.Email == "izleyici@wms.local", cancellationToken);
        if (!hasViewerUser)
        {
            await context.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = "izleyici@wms.local",
                PasswordHash = passwordHasher.HashPassword("Viewer@123"),
                FirstName = "Demo",
                LastName = "Izleyici",
                IsActive = true,
                RoleId = viewerRole.Id
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
