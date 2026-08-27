using CashApp.Application.Anomalies.Dtos;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;

namespace CashApp.Application.Anomalies;

public interface IAnomalyService
{
    Task<PagedResponse<AnomalyListItemDto>> ListAsync(AnomalyFilterDto filter, CancellationToken ct = default);
    Task<AnomalyDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<AnomalyDetailDto> CreateAsync(CreateAnomalyDto dto, CancellationToken ct = default);
    Task<AnomalyDetailDto> AssignAsync(int id, AssignAnomalyDto dto, CancellationToken ct = default);
    Task<AnomalyDetailDto> ResolveAsync(int id, ResolveAnomalyDto dto, CancellationToken ct = default);
    Task<AnomalyDetailDto> AddCommentAsync(int id, AddAnomalyCommentDto dto, CancellationToken ct = default);

    // Helper pour création auto par d'autres services.
    Task<AnomalyCase> CreateAutoAsync(string title, string? description, AnomalySeverity severity,
        string? relatedEntityType, int? relatedEntityId, int? cashRegisterId, int? cashSessionId,
        CancellationToken ct = default);
}
