using CashApp.Application.Approvals.Dtos;
using CashApp.Application.Common.Models;
using CashApp.Domain.Entities.V2;
using CashApp.Domain.Enums;

namespace CashApp.Application.Approvals;

public interface IApprovalService
{
    Task<PagedResponse<ApprovalRequestListItemDto>> ListAsync(ApprovalRequestFilterDto filter, CancellationToken ct = default);
    Task<ApprovalRequestDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<ApprovalRequestDetailDto> ApproveAsync(int id, ApproveRequestDto dto, CancellationToken ct = default);
    Task<ApprovalRequestDetailDto> RejectAsync(int id, RejectRequestDto dto, CancellationToken ct = default);

    // Helpers utilisés par les services métier pour créer des demandes.
    Task<ApprovalRequest?> FindMatchingRuleAsync(ApprovalTargetType targetType, decimal? amount, string? currency, CancellationToken ct = default);
    Task<ApprovalRequest> CreateRequestAsync(int approvalRuleId, ApprovalTargetType targetType, string targetEntityType, int targetEntityId,
        int? cashRegisterId, decimal? amount, string? currency, string reason, CancellationToken ct = default);
}
