namespace CashApp.Application.Common.Interfaces;

public interface IReferenceGeneratorService
{
    Task<string> NextOperationRefAsync(int cashRegisterId, DateTime operationDate, CancellationToken ct = default);
}
