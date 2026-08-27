using CashApp.Application.AuditLogs.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogListItemDto>> ListAsync(AuditLogFilterDto filter, CancellationToken ct = default);
    Task<AuditLogDetailDto> GetAsync(int id, CancellationToken ct = default);
}
