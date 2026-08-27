using CashApp.Domain.Enums;

namespace CashApp.Application.Common.Interfaces;

// Service centralisé d'écriture d'audit logs.
// Les implémentations doivent appeler SaveChanges (ou s'inscrire dans la transaction du contexte).
public interface IAuditLogger
{
    Task LogAsync(AuditAction action, string entityType, int? entityId,
        string? description = null,
        object? oldValues = null,
        object? newValues = null,
        object? metadata = null,
        CancellationToken ct = default);
}
