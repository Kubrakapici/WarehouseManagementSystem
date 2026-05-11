using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Categories;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff},{RoleNames.Operations},{RoleNames.Viewer}")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetTree([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var data = await _categories.GetTreeAsync(search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _categories.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound(ApiResponse<CategoryDto>.Fail("Kategori bulunamadı.")) : Ok(ApiResponse<CategoryDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
    {
        var created = await _categories.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<CategoryDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
    {
        var updated = await _categories.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<CategoryDto>.Fail("Kategori bulunamadı.")) : Ok(ApiResponse<CategoryDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _categories.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (alt kategori veya ürün bağlantısı olabilir)."));
    }
}
