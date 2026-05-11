using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Categories;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IRepository<Category> categories, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetTreeAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _categories.Query()
            .Include(c => c.ParentCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(s));
        }

        var list = await query.OrderBy(c => c.ParentCategoryId).ThenBy(c => c.Name).ToListAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<CategoryDto>>(list);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.Query().Include(c => c.ParentCategory).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<CategoryDto>(entity);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ParentCategoryId.HasValue &&
            !await _categories.ExistsAsync(c => c.Id == dto.ParentCategoryId.Value, cancellationToken))
            throw new InvalidOperationException("Üst kategori bulunamadı.");

        var entity = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            ParentCategoryId = dto.ParentCategoryId
        };
        await _categories.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(await _categories.Query().Include(c => c.ParentCategory).FirstAsync(c => c.Id == entity.Id, cancellationToken));
    }

    public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null) return null;

        if (dto.ParentCategoryId == id)
            throw new InvalidOperationException("Kategori kendi üst kategorisi olamaz.");

        if (dto.ParentCategoryId.HasValue &&
            !await _categories.ExistsAsync(c => c.Id == dto.ParentCategoryId.Value, cancellationToken))
            throw new InvalidOperationException("Üst kategori bulunamadı.");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description;
        entity.ParentCategoryId = dto.ParentCategoryId;
        _categories.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<CategoryDto>(await _categories.Query().Include(c => c.ParentCategory).FirstAsync(c => c.Id == id, cancellationToken));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hasChildren = await _categories.ExistsAsync(c => c.ParentCategoryId == id, cancellationToken);
        if (hasChildren) return false;

        var entity = await _categories.Query().Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null) return false;

        if (entity.Products.Count > 0) return false;

        _categories.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
