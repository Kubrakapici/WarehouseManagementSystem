using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Auth;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AuthController(
        IAuthService authService,
        IRepository<User> users,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _authService = authService;
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>Giriş işlemi — JWT access ve refresh token döner.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result == null
            ? Unauthorized(ApiResponse<LoginResponseDto>.Fail("E-posta veya şifre hatalı."))
            : Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    /// <summary>Refresh token ile yeni access token üretir.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        return result == null
            ? Unauthorized(ApiResponse<LoginResponseDto>.Fail("Geçersiz veya süresi dolmuş oturum."))
            : Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    /// <summary>Oturumu sonlandırır (refresh token temizlenir).</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            _users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Ok(ApiResponse.Ok("Çıkış yapıldı."));
    }
}
