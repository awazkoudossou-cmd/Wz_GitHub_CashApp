using CashApp.Application.Common.Interfaces;
using CashApp.Application.Exports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashApp.Api.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize]
public class ExportsController : ControllerBase
{
    private readonly IExportService _export;
    public ExportsController(IExportService export) => _export = export;

    [HttpPost("operations")]
    public async Task<IActionResult> Operations([FromBody] ExportOperationsRequestDto dto, CancellationToken ct)
    {
        var fmt = (dto.Format ?? "xlsx").ToLowerInvariant();
        if (fmt == "pdf")
        {
            var pdf = await _export.ExportOperationsPdfAsync(dto.From, dto.To, dto.CashRegisterId, ct);
            return File(pdf, "application/pdf", $"operations_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.pdf");
        }
        var xlsx = await _export.ExportOperationsExcelAsync(dto.From, dto.To, dto.CashRegisterId,
            dto.Direction, dto.IncludeDeleted, ct);
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"operations_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.xlsx");
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Sessions([FromBody] ExportSessionsRequestDto dto, CancellationToken ct)
    {
        var fmt = (dto.Format ?? "xlsx").ToLowerInvariant();
        if (fmt == "pdf")
        {
            var pdf = await _export.ExportSessionsPdfAsync(dto.From, dto.To, dto.CashRegisterId, ct);
            return File(pdf, "application/pdf", $"sessions_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.pdf");
        }
        var xlsx = await _export.ExportSessionsExcelAsync(dto.From, dto.To, dto.CashRegisterId, ct);
        return File(xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"sessions_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.xlsx");
    }

    [HttpPost("cash-state")]
    public async Task<IActionResult> CashState([FromBody] ExportCashStateRequestDto dto, CancellationToken ct)
    {
        var pdf = await _export.ExportCashStatePdfAsync(dto.CashSessionId, ct);
        return File(pdf, "application/pdf", $"cash_state_{dto.CashSessionId}.pdf");
    }

    // Reçu PDF A5 (1 ou 2 copies selon le setting RECEIPT_COPIES_COUNT)
    [HttpGet("operations/{id:int}/receipt")]
    public async Task<IActionResult> OperationReceipt(int id, CancellationToken ct)
    {
        var pdf = await _export.ExportOperationReceiptPdfAsync(id, ct);
        return File(pdf, "application/pdf", $"recu_op_{id}.pdf");
    }

    [HttpGet("approval-requests/{id:int}/pdf")]
    public async Task<IActionResult> ApprovalRequestPdf(int id, CancellationToken ct)
    {
        var pdf = await _export.ExportApprovalRequestPdfAsync(id, ct);
        return File(pdf, "application/pdf", $"approval_request_{id}.pdf");
    }
}
