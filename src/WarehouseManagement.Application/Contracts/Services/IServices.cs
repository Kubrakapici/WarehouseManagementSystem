using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.DTOs.Auth;
using WarehouseManagement.Application.DTOs.Categories;
using WarehouseManagement.Application.DTOs.Customers;
using WarehouseManagement.Application.DTOs.Dashboard;
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
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Contracts.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto?> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetTreeAsync(string? search, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PaginationParameters parameters, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWarehouseService
{
    Task<PagedResult<WarehouseDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ILocationService
{
    Task<PagedResult<LocationDto>> GetPagedAsync(PaginationParameters parameters, Guid? warehouseId, CancellationToken cancellationToken = default);
    Task<LocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LocationDto> CreateAsync(CreateLocationDto dto, CancellationToken cancellationToken = default);
    Task<LocationDto?> UpdateAsync(Guid id, UpdateLocationDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IStockService
{
    Task<PagedResult<StockBalanceDto>> GetBalancesPagedAsync(PaginationParameters parameters, Guid? warehouseId, CancellationToken cancellationToken = default);
    Task<PagedResult<StockMovementDto>> GetMovementsPagedAsync(PaginationParameters parameters, Guid? productId, StockMovementType? type, CancellationToken cancellationToken = default);
    Task StockEntryAsync(StockEntryRequestDto dto, CancellationToken cancellationToken = default);
    Task StockExitAsync(StockExitRequestDto dto, CancellationToken cancellationToken = default);
    Task StockTransferAsync(StockTransferRequestDto dto, CancellationToken cancellationToken = default);
    Task StockCountAsync(StockCountRequestDto dto, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetPagedAsync(PaginationParameters parameters, OrderStatus? status, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}

public interface IPurchaseRequisitionService
{
    Task<PagedResult<PurchaseRequisitionDto>> GetPagedAsync(PaginationParameters parameters, PurchaseRequisitionStatus? status, Guid? warehouseId, CancellationToken cancellationToken = default);
    Task<PurchaseRequisitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseRequisitionDto> CreateDraftAsync(CreatePurchaseRequisitionDto dto, CancellationToken cancellationToken = default);
    Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid id, string? reason, CancellationToken cancellationToken = default);
    Task<SupplierQuoteDto> AddSupplierQuoteAsync(Guid requisitionId, AddSupplierQuoteDto dto, CancellationToken cancellationToken = default);
    Task AcceptSupplierQuoteAsync(Guid requisitionId, Guid quoteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseSuggestionLineDto>> GetSuggestionsAsync(Guid? warehouseId, CancellationToken cancellationToken = default);
}

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<SupplierDto?> UpdateAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(PaginationParameters parameters, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PaginationParameters parameters, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<byte[]> ExportCriticalStockExcelAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportDailyStockMovementExcelAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<byte[]> ExportProductMovementPdfAsync(Guid productId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}

public interface IProductExcelService
{
    Task<byte[]> ExportProductsAsync(CancellationToken cancellationToken = default);
    Task<(int Imported, IReadOnlyList<string> Errors)> ImportProductsAsync(Stream stream, CancellationToken cancellationToken = default);
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
