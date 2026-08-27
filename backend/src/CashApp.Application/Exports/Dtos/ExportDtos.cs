namespace CashApp.Application.Exports.Dtos;

public record ExportOperationsRequestDto(
    DateTime From,
    DateTime To,
    int? CashRegisterId,
    string Format, // "xlsx" | "pdf"
    string? Direction = null,       // "IN" | "OUT" | null = toutes
    bool IncludeDeleted = false);   // inclut les opérations annulées/rejetées (soft-deleted)

public record ExportSessionsRequestDto(
    DateTime From,
    DateTime To,
    int? CashRegisterId,
    string Format); // "xlsx" | "pdf"

public record ExportCashStateRequestDto(int CashSessionId);

public record ExportFileDto(
    string FileName,
    string ContentType,
    byte[] Content);
