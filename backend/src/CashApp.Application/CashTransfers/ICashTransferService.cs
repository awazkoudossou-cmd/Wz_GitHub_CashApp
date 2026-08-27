using CashApp.Application.CashTransfers.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.CashTransfers;

public interface ICashTransferService
{
    Task<PagedResponse<CashTransferListItemDto>> ListAsync(CashTransferFilterDto filter, CancellationToken ct = default);
    Task<CashTransferDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<CashTransferDetailDto> CreateAsync(CreateCashTransferDto dto, CancellationToken ct = default);
    Task<CashTransferDetailDto> CompleteAsync(int id, CancellationToken ct = default);
    Task<CashTransferDetailDto> CancelAsync(int id, CancelCashTransferDto dto, CancellationToken ct = default);
}
