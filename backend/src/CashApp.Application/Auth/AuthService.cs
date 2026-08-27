using CashApp.Application.Auth.Dtos;
using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashApp.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ISettingsService _settings;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt, ISettingsService settings)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _settings = settings;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct)
            ?? throw new BusinessRuleException("INVALID_CREDENTIALS", "Identifiants invalides.");

        if (!user.IsActive)
            throw new BusinessRuleException("USER_INACTIVE", "Utilisateur désactivé.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleException("INVALID_CREDENTIALS", "Identifiants invalides.");

        var token = _jwt.GenerateToken(user, out var expiresAt);
        var context = await BuildContextAsync(user, ct);

        return new LoginResponseDto(
            Token: token,
            ExpiresAt: expiresAt,
            User: context.User,
            CashRegisters: context.CashRegisters,
            Features: context.Features,
            AppMode: context.AppMode);
    }

    public async Task<LoginResponseDto> GetCurrentContextAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);
        var context = await BuildContextAsync(user, ct);
        return new LoginResponseDto(
            Token: string.Empty,
            ExpiresAt: default,
            User: context.User,
            CashRegisters: context.CashRegisters,
            Features: context.Features,
            AppMode: context.AppMode);
    }

    private async Task<(CurrentUserDto User, IReadOnlyList<CurrentCashRegisterDto> CashRegisters,
                       IReadOnlyList<FeatureDto> Features, string AppMode)>
        BuildContextAsync(User user, CancellationToken ct)
    {
        var registers = await _db.UserCashRegisters
            .AsNoTracking()
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => new CurrentCashRegisterDto(
                uc.CashRegister.Id, uc.CashRegister.Code, uc.CashRegister.Name,
                uc.CashRegister.CurrencyCode, uc.CashRegister.IsActive))
            .ToListAsync(ct);

        // ADMIN et SUPERVISOR voient toutes les caisses (accès transverses : Comptabilité, rapports, etc.),
        // même sans affectation explicite via UserCashRegister.
        if (user.RoleCode is Domain.Constants.RoleCodes.Admin or Domain.Constants.RoleCodes.Supervisor)
        {
            registers = await _db.CashRegisters
                .AsNoTracking()
                .OrderBy(r => r.Code)
                .Select(r => new CurrentCashRegisterDto(r.Id, r.Code, r.Name, r.CurrencyCode, r.IsActive))
                .ToListAsync(ct);
        }

        var features = await _db.FeatureSettings
            .AsNoTracking()
            .OrderBy(f => f.FeatureCode)
            .Select(f => new FeatureDto(f.FeatureCode, f.FeatureName, f.IsEnabled))
            .ToListAsync(ct);

        var appMode = (await _settings.GetAppModeAsync(ct)).Mode.ToString();

        var dto = new CurrentUserDto(user.Id, user.Username, user.FullName, user.RoleCode, user.IsActive);
        return (dto, registers, features, appMode);
    }
}
