using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Application.Contracts;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
