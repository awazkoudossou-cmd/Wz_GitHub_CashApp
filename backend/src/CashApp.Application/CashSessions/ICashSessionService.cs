using CashApp.Application.CashSessions.Dtos;

namespace CashApp.Application.CashSessions;

public interface ICashSessionService
{
    Task<IReadOnlyList<CashSessionListItemDto>> ListAsync(int? cashRegisterId, CancellationToken ct = default);
    Task<CashSessionDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<CashSessionDetailDto> OpenAsync(OpenCashSessionDto dto, CancellationToken ct = default);
    Task<CashSessionDetailDto> CloseAsync(int id, CloseCashSessionDto dto, CancellationToken ct = default);
    Task<decimal> RecomputeTheoreticalBalanceAsync(int sessionId, CancellationToken ct = default);
    Task<OpeningDefaultDto> GetOpeningDefaultAsync(int cashRegisterId, CancellationToken ct = default);
    Task<SessionPendingItemsDto> GetPendingItemsAsync(int sessionId, CancellationToken ct = default);
}
