using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Orders;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<Product> _products;
    private readonly IRepository<Supplier> _suppliers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRealtimeNotifier _realtime;

    public OrderService(
        IRepository<Order> orders,
        IRepository<Product> products,
        IRepository<Supplier> suppliers,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRealtimeNotifier realtime)
    {
        _orders = orders;
        _products = products;
        _suppliers = suppliers;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _realtime = realtime;
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(PaginationParameters parameters, OrderStatus? status, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> filtered = _orders.Query()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product);

        if (status.HasValue)
            filtered = filtered.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            filtered = filtered.Where(o => o.OrderNumber.ToLower().Contains(s));
        }

        filtered = parameters.SortDescending ? filtered.OrderByDescending(o => o.CreatedDate) : filtered.OrderBy(o => o.CreatedDate);

        var page = await filtered.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(o => _mapper.Map<OrderDto>(o)).ToList();

        return new PagedResult<OrderDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orders.Query()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.SupplierId.HasValue &&
            !await _suppliers.ExistsAsync(s => s.Id == dto.SupplierId.Value, cancellationToken))
            throw new InvalidOperationException("Tedarikçi bulunamadı.");

        foreach (var item in dto.Items)
        {
            if (!await _products.ExistsAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken))
                throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = await GenerateUniqueOrderNumberAsync(cancellationToken),
            Status = OrderStatus.Pending,
            Notes = dto.Notes,
            SupplierId = dto.SupplierId
        };

        foreach (var item in dto.Items)
        {
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
        await _realtime.NotifyOrderStatusChangedAsync(order.Id, order.Status, cancellationToken);

        return (await GetByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _orders.Query().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null) return null;

        order.Status = status;
        _orders.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
        await _realtime.NotifyOrderStatusChangedAsync(order.Id, order.Status, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<string> GenerateUniqueOrderNumberAsync(CancellationToken cancellationToken)
    {
        var rnd = new Random();
        for (var i = 0; i < 50; i++)
        {
            var num = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{rnd.Next(1000, 9999)}";
            if (!await _orders.ExistsAsync(o => o.OrderNumber == num, cancellationToken))
                return num;
        }

        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}
