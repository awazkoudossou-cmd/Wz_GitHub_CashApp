using CashApp.Application.CashRegisters.Dtos;

namespace CashApp.Application.CashRegisters;

public interface ICashRegisterService
{
    Task<IReadOnlyList<CashRegisterListItemDto>> ListAsync(CancellationToken ct = default);
    Task<CashRegisterDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<CashRegisterDetailDto> CreateAsync(CreateCashRegisterDto dto, CancellationToken ct = default);
    Task<CashRegisterDetailDto> UpdateAsync(int id, UpdateCashRegisterDto dto, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, UpdateCashRegisterStatusDto dto, CancellationToken ct = default);
}
