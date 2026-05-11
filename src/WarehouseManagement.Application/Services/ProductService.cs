using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Products;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class ProductService : IProductService
{
    private readonly IRepository<Product> _products;
    private readonly IRepository<Category> _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(
        IRepository<Product> products,
        IRepository<Category> categories,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(PaginationParameters parameters, Guid? categoryId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _products.Query().Include(p => p.Category).Include(p => p.StockBalances).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Sku.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "sku" => parameters.SortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "name" => parameters.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => parameters.SortDescending ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate)
        };

        var page = await query.ToPagedResultAsync(parameters.PageNumber, parameters.PageSize, cancellationToken);
        var dtoItems = page.Items.Select(p =>
        {
            var dto = _mapper.Map<ProductDto>(p);
            dto.TotalQuantity = p.StockBalances.Sum(b => b.Quantity);
            return dto;
        }).ToList();

        return new PagedResult<ProductDto>
        {
            Items = dtoItems,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _products.Query().Include(x => x.Category).Include(x => x.StockBalances).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (p == null) return null;
        var dto = _mapper.Map<ProductDto>(p);
        dto.TotalQuantity = p.StockBalances.Sum(b => b.Quantity);
        return dto;
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var code = barcode.Trim();
        var p = await _products.Query().Include(x => x.Category).Include(x => x.StockBalances)
            .FirstOrDefaultAsync(x => x.Barcode == code, cancellationToken);
        if (p == null) return null;
        var dto = _mapper.Map<ProductDto>(p);
        dto.TotalQuantity = p.StockBalances.Sum(b => b.Quantity);
        return dto;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _categories.ExistsAsync(c => c.Id == dto.CategoryId, cancellationToken))
            throw new InvalidOperationException("Kategori bulunamadı.");

        var sku = dto.Sku.Trim();
        if (await _products.ExistsAsync(p => p.Sku == sku, cancellationToken))
            throw new InvalidOperationException("SKU benzersiz olmalıdır.");

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Sku = sku,
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            ImageUrl = dto.ImageUrl,
            UnitPrice = dto.UnitPrice,
            MinimumStockLevel = dto.MinimumStockLevel,
            IsActive = dto.IsActive,
            CategoryId = dto.CategoryId,
            QrCodeData = dto.GenerateQr ? $"WMS:{sku}" : null
        };

        await _products.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _products.Query().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity == null) return null;

        if (!await _categories.ExistsAsync(c => c.Id == dto.CategoryId, cancellationToken))
            throw new InvalidOperationException("Kategori bulunamadı.");

        var sku = dto.Sku.Trim();
        if (await _products.ExistsAsync(p => p.Sku == sku && p.Id != id, cancellationToken))
            throw new InvalidOperationException("SKU benzersiz olmalıdır.");

        entity.Name = dto.Name.Trim();
        entity.Sku = sku;
        entity.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        entity.ImageUrl = dto.ImageUrl;
        entity.UnitPrice = dto.UnitPrice;
        entity.MinimumStockLevel = dto.MinimumStockLevel;
        entity.IsActive = dto.IsActive;
        entity.CategoryId = dto.CategoryId;
        if (string.IsNullOrEmpty(entity.QrCodeData))
            entity.QrCodeData = $"WMS:{sku}";

        _products.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _products.Query().Include(p => p.StockBalances).Include(p => p.OrderItems).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity == null) return false;
        if (entity.StockBalances.Any(b => b.Quantity != 0)) return false;
        if (entity.OrderItems.Any()) return false;

        _products.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
