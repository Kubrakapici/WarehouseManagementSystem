using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Users;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{RoleNames.Admin}")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetPaged([FromQuery] PaginationParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _users.GetPagedAsync(parameters, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user == null ? NotFound(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı.")) : Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var created = await _users.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<UserDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var updated = await _users.UpdateAsync(id, dto, cancellationToken);
        return updated == null ? NotFound(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı.")) : Ok(ApiResponse<UserDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _users.DeleteAsync(id, cancellationToken);
        return ok ? Ok(ApiResponse.Ok("Silindi.")) : BadRequest(ApiResponse.Fail("Silinemedi (kendinizi veya bağlı kayıtları silemezsiniz)."));
    }
}
