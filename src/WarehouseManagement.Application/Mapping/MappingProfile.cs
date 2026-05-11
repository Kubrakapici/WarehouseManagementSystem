using AutoMapper;
using WarehouseManagement.Application.DTOs.Categories;
using WarehouseManagement.Application.DTOs.Customers;
using WarehouseManagement.Application.DTOs.Locations;
using WarehouseManagement.Application.DTOs.Notifications;
using WarehouseManagement.Application.DTOs.Orders;
using WarehouseManagement.Application.DTOs.Procurement;
using WarehouseManagement.Application.DTOs.Products;
using WarehouseManagement.Application.DTOs.Roles;
using WarehouseManagement.Application.DTOs.Stock;
using WarehouseManagement.Application.DTOs.Suppliers;
using WarehouseManagement.Application.DTOs.Users;
using WarehouseManagement.Application.DTOs.Warehouses;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Role, RoleDto>();

        CreateMap<User, UserDto>()
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Role.Name))
            .ForMember(d => d.WarehouseIds, o => o.MapFrom(s => s.UserWarehouses.Select(w => w.WarehouseId).ToList()));

        CreateMap<PurchaseRequisitionLine, PurchaseRequisitionLineDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Product.Sku));

        CreateMap<SupplierQuote, SupplierQuoteDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name));

        CreateMap<PurchaseRequisition, PurchaseRequisitionDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.RequestedByName, o => o.MapFrom(s => $"{s.RequestedByUser.FirstName} {s.RequestedByUser.LastName}".Trim()))
            .ForMember(d => d.ApprovedByName, o => o.MapFrom(s => s.ApprovedByUser != null ? $"{s.ApprovedByUser.FirstName} {s.ApprovedByUser.LastName}".Trim() : null))
            .ForMember(d => d.Lines, o => o.MapFrom(s => s.Lines))
            .ForMember(d => d.Quotes, o => o.MapFrom(s => s.SupplierQuotes));

        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ParentCategoryName, o => o.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.TotalQuantity, o => o.Ignore());

        CreateMap<Warehouse, WarehouseDto>()
            .ForMember(d => d.LocationCount, o => o.MapFrom(s => s.Locations.Count));

        CreateMap<Location, LocationDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name));

        CreateMap<StockBalance, StockBalanceDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code));

        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(d => d.MovementType, o => o.MapFrom(s => s.MovementType.ToString()))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.FromLocationCode, o => o.MapFrom(s => s.FromLocation != null ? s.FromLocation.Code : null))
            .ForMember(d => d.ToLocationCode, o => o.MapFrom(s => s.ToLocation != null ? s.ToLocation.Code : null))
            .ForMember(d => d.UserFullName, o => o.MapFrom(s => $"{s.User.FirstName} {s.User.LastName}".Trim()));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

        CreateMap<Supplier, SupplierDto>()
            .ForMember(d => d.OrderCount, o => o.MapFrom(s => s.Orders.Count));

        CreateMap<Customer, CustomerDto>();

        CreateMap<Notification, NotificationDto>();
    }
}
