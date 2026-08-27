using System.Text.Json;
using CashApp.Application.Common.Interfaces;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;

namespace CashApp.Application.Audit;

public class AuditLogger : IAuditLogger
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IHttpContextInfo _httpInfo;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditLogger(IAppDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IHttpContextInfo httpInfo)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _httpInfo = httpInfo;
    }

    public Task LogAsync(AuditAction action, string entityType, int? entityId,
        string? description = null, object? oldValues = null, object? newValues = null, object? metadata = null,
        CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActionType = action,
            EntityType = entityType,
            EntityId = entityId,
            PerformedBy = _currentUser.UserId,
            PerformedAt = _clock.UtcNow,
            Description = description,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOpts),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOpts),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOpts),
            IpAddress = _httpInfo.RemoteIp
        });
        // NB : l'appelant fait SaveChangesAsync.
        return Task.CompletedTask;
    }
}
