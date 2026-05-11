using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Procurement;
using WarehouseManagement.Domain.Entities;
using WarehouseManagement.Domain.Enums;

namespace WarehouseManagement.Application.Services;

public class PurchaseRequisitionService : IPurchaseRequisitionService
{
    private readonly IRepository<PurchaseRequisition> _requisitions;
    private readonly IRepository<Product> _products;
    private readonly IRepository<Warehouse> _warehouses;
    private readonly IRepository<Supplier> _suppliers;
    private readonly IRepository<SupplierQuote> _quotes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IWarehouseAccessService _warehouseAccess;
    private readonly IRealtimeNotifier _realtime;

    public PurchaseRequisitionService(
        IRepository<PurchaseRequisition> requisitions,
        IRepository<Product> products,
        IRepository<Warehouse> warehouses,
        IRepository<Supplier> suppliers,
        IRepository<SupplierQuote> quotes,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser,
        IWarehouseAccessService warehouseAccess,
        IRealtimeNotifier realtime)
    {
        _requisitions = requisitions;
        _products = products;
        _warehouses = warehouses;
        _suppliers = suppliers;
        _quotes = quotes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _warehouseAccess = warehouseAccess;
        _realtime = realtime;
    }

    public async Task<PagedResult<PurchaseRequisitionDto>> GetPagedAsync(
        PaginationParameters parameters,
        PurchaseRequisitionStatus? status,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);
        var query = _requisitions.Query()
            .Include(r => r.Warehouse)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .Include(r => r.SupplierQuotes).ThenInclude(q => q.Supplier)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (warehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == warehouseId.Value);

        if (restricted != null)
            query = query.Where(r => restricted.Contains(r.WarehouseId));

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(r => r.RequestNumber.ToLower().Contains(s) || (r.Title != null && r.Title.ToLower().Contains(s)));
        }

        query = parameters.SortDescending ? query.OrderByDescending(r => r.CreatedDate) : query.OrderBy(r => r.CreatedDate);

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var items = page.Items.Select(r => _mapper.Map<PurchaseRequisitionDto>(r)).ToList();

        return new PagedResult<PurchaseRequisitionDto>
        {
            Items = items,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<PurchaseRequisitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);
        var entity = await _requisitions.Query()
            .Include(r => r.Warehouse)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .Include(r => r.SupplierQuotes).ThenInclude(q => q.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity == null) return null;
        if (restricted != null && !restricted.Contains(entity.WarehouseId))
            return null;

        return _mapper.Map<PurchaseRequisitionDto>(entity);
    }

    public async Task<PurchaseRequisitionDto> CreateDraftAsync(CreatePurchaseRequisitionDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        await _warehouseAccess.EnsureCanAccessWarehouseAsync(dto.WarehouseId, cancellationToken);

        if (!await _warehouses.ExistsAsync(w => w.Id == dto.WarehouseId && w.IsActive, cancellationToken))
            throw new InvalidOperationException("Depo bulunamadi veya pasif.");

        if (dto.Lines.Count == 0)
            throw new InvalidOperationException("En az bir satir gerekli.");

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Miktar sifirdan buyuk olmali.");
            if (!await _products.ExistsAsync(p => p.Id == line.ProductId && p.IsActive, cancellationToken))
                throw new InvalidOperationException($"Urun bulunamadi: {line.ProductId}");
        }

        var entity = new PurchaseRequisition
        {
            Id = Guid.NewGuid(),
            RequestNumber = await GenerateRequestNumberAsync(cancellationToken),
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            WarehouseId = dto.WarehouseId,
            Status = PurchaseRequisitionStatus.Draft,
            RequestedByUserId = userId
        };

        foreach (var line in dto.Lines)
        {
            entity.Lines.Add(new PurchaseRequisitionLine
            {
                Id = Guid.NewGuid(),
                PurchaseRequisitionId = entity.Id,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        await _requisitions.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadForMutationAsync(id, cancellationToken);
        if (entity.Status != PurchaseRequisitionStatus.Draft)
            throw new InvalidOperationException("Sadece taslak talepler gonderilebilir.");

        entity.Status = PurchaseRequisitionStatus.PendingApproval;
        _requisitions.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadForMutationAsync(id, cancellationToken);
        if (entity.Status != PurchaseRequisitionStatus.PendingApproval)
            throw new InvalidOperationException("Sadece onay bekleyen talepler onaylanabilir.");

        var approverId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        entity.Status = PurchaseRequisitionStatus.Approved;
        entity.ApprovedByUserId = approverId;
        entity.ApprovedDate = DateTime.UtcNow;
        _requisitions.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var entity = await LoadForMutationAsync(id, cancellationToken);
        if (entity.Status != PurchaseRequisitionStatus.PendingApproval)
            throw new InvalidOperationException("Sadece onay bekleyen talepler reddedilebilir.");

        var approverId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        entity.Status = PurchaseRequisitionStatus.Rejected;
        entity.ApprovedByUserId = approverId;
        entity.ApprovedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
            entity.Notes = string.IsNullOrWhiteSpace(entity.Notes) ? $"Red: {reason}" : $"{entity.Notes}\nRed: {reason}";

        _requisitions.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
    }

    public async Task<SupplierQuoteDto> AddSupplierQuoteAsync(Guid requisitionId, AddSupplierQuoteDto dto, CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);
        var status = await _requisitions.Query()
            .Where(r => r.Id == requisitionId)
            .Select(r => new { r.Status, r.WarehouseId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Talep bulunamadi.");

        if (restricted != null && !restricted.Contains(status.WarehouseId))
            throw new UnauthorizedAccessException();

        if (status.Status != PurchaseRequisitionStatus.Approved)
            throw new InvalidOperationException("Teklif sadece onaylanmis taleplere eklenebilir.");

        if (dto.TotalAmount <= 0)
            throw new InvalidOperationException("Tutar sifirdan buyuk olmali.");

        var supplier = await _suppliers.Query().AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.IsActive, cancellationToken)
                       ?? throw new InvalidOperationException("Tedarikci bulunamadi.");

        var quote = new SupplierQuote
        {
            Id = Guid.NewGuid(),
            PurchaseRequisitionId = requisitionId,
            SupplierId = dto.SupplierId,
            TotalAmount = dto.TotalAmount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency!.Trim().ToUpperInvariant(),
            Status = SupplierQuoteStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };

        await _quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SupplierQuoteDto
        {
            Id = quote.Id,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            TotalAmount = quote.TotalAmount,
            Currency = quote.Currency,
            Status = quote.Status
        };
    }

    public async Task AcceptSupplierQuoteAsync(Guid requisitionId, Guid quoteId, CancellationToken cancellationToken = default)
    {
        var entity = await LoadForMutationAsync(requisitionId, cancellationToken);
        if (entity.Status != PurchaseRequisitionStatus.Approved)
            throw new InvalidOperationException("Gecersiz talep durumu.");

        _ = entity.SupplierQuotes.FirstOrDefault(q => q.Id == quoteId)
            ?? throw new InvalidOperationException("Teklif bulunamadi.");

        foreach (var q in entity.SupplierQuotes)
            q.Status = q.Id == quoteId ? SupplierQuoteStatus.Accepted : SupplierQuoteStatus.Rejected;

        entity.Status = PurchaseRequisitionStatus.Completed;
        _requisitions.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtime.NotifyDashboardChangedAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseSuggestionLineDto>> GetSuggestionsAsync(Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);
        if (warehouseId.HasValue)
            await _warehouseAccess.EnsureCanAccessWarehouseAsync(warehouseId.Value, cancellationToken);

        var products = await _products.Query()
            .Include(p => p.StockBalances).ThenInclude(b => b.Location)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var list = new List<PurchaseSuggestionLineDto>();
        foreach (var p in products)
        {
            var qty = p.StockBalances
                .Where(b => warehouseId == null || b.Location.WarehouseId == warehouseId.Value)
                .Where(b => restricted == null || restricted.Contains(b.Location.WarehouseId))
                .Sum(b => b.Quantity);

            if (qty > p.MinimumStockLevel)
                continue;

            var suggested = Math.Max(p.MinimumStockLevel - qty, p.MinimumStockLevel);
            if (suggested <= 0)
                suggested = p.MinimumStockLevel;

            list.Add(new PurchaseSuggestionLineDto
            {
                ProductId = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                CurrentQuantity = qty,
                MinimumStockLevel = p.MinimumStockLevel,
                SuggestedOrderQuantity = suggested
            });
        }

        return list.OrderBy(x => x.CurrentQuantity).ThenBy(x => x.Name).Take(100).ToList();
    }

    private async Task<PurchaseRequisition> LoadForMutationAsync(Guid id, CancellationToken cancellationToken)
    {
        var restricted = await _warehouseAccess.GetRestrictedWarehouseIdsAsync(cancellationToken);
        var entity = await _requisitions.Query()
            .Include(r => r.SupplierQuotes)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                   ?? throw new InvalidOperationException("Talep bulunamadi.");

        if (restricted != null && !restricted.Contains(entity.WarehouseId))
            throw new UnauthorizedAccessException();

        return entity;
    }

    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken)
    {
        var rnd = new Random();
        for (var i = 0; i < 50; i++)
        {
            var num = $"PR-{DateTime.UtcNow:yyyyMMdd}-{rnd.Next(1000, 9999)}";
            if (!await _requisitions.ExistsAsync(r => r.RequestNumber == num, cancellationToken))
                return num;
        }

        return $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}
