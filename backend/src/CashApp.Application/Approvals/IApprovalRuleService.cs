using CashApp.Application.Approvals.Dtos;

namespace CashApp.Application.Approvals;

public interface IApprovalRuleService
{
    Task<IReadOnlyList<ApprovalRuleDto>> ListAsync(CancellationToken ct = default);
    Task<ApprovalRuleDto> CreateAsync(CreateApprovalRuleDto dto, CancellationToken ct = default);
    Task<ApprovalRuleDto> UpdateAsync(int id, UpdateApprovalRuleDto dto, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, UpdateApprovalRuleStatusDto dto, CancellationToken ct = default);
}
