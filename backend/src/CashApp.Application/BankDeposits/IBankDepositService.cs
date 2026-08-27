using CashApp.Application.BankDeposits.Dtos;
using CashApp.Application.Common.Models;

namespace CashApp.Application.BankDeposits;

public interface IBankDepositService
{
    Task<PagedResponse<BankDepositListItemDto>> ListAsync(BankDepositFilterDto filter, CancellationToken ct = default);
    Task<BankDepositDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<BankDepositDetailDto> CreateAsync(CreateBankDepositDto dto, CancellationToken ct = default);
    Task<BankDepositDetailDto> CompleteAsync(int id, CancellationToken ct = default);
    Task<BankDepositDetailDto> CancelAsync(int id, CancelBankDepositDto dto, CancellationToken ct = default);
}
