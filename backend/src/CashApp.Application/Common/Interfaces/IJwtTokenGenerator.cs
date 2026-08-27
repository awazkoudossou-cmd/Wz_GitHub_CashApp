using CashApp.Domain.Entities;

namespace CashApp.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, out DateTime expiresAt);
}
