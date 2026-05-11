using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarehouseManagement.Application.Common;
using WarehouseManagement.Application.Contracts;
using WarehouseManagement.Application.Contracts.Repositories;
using WarehouseManagement.Application.Contracts.Services;
using WarehouseManagement.Application.DTOs.Auth;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwt;

    public AuthService(
        IRepository<User> users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwt)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var access = _jwtTokenService.GenerateAccessToken(user);
        var refresh = _jwtTokenService.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Role = user.Role.Name
        };
    }

    public async Task<LoginResponseDto?> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow || !user.IsActive)
            return null;

        var access = _jwtTokenService.GenerateAccessToken(user);
        var refresh = _jwtTokenService.GenerateRefreshToken();
        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Role = user.Role.Name
        };
    }
}
