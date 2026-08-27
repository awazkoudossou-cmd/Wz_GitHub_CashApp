using ClosedXML.Excel;

namespace CashApp.Infrastructure.Services;

public record AccountingLedgerExportRow(
    DateTime EntryDate,
    DateTime OperationDate,
    string JournalCode,
    string Reference,
    string? PieceNumber,
    string AccountNumber,
    string AccountName,
    string Description,
    decimal Debit,
    decimal Credit,
    string UserName,
    string BatchReference);

public record AccountingLedgerExportModel(
    IReadOnlyList<AccountingLedgerExportRow> Rows,
    DateTime ExportDate,
    string ExportedByName,
    string FilterDescription);

public interface IExcelExportService
{
    byte[] BuildLedgerWorkbook(AccountingLedgerExportModel model);
}

// Construction du fichier Excel du "Brouillard Comptable" via ClosedXML (wrapper OpenXML, pas d'Interop Office).
public class ExcelExportService : IExcelExportService
{
    private static readonly string[] Headers =
    {
        "Date saisie", "Date opération", "Code journal", "Référence", "Numéro de pièce",
        "Compte", "Libellé", "Débit", "Crédit", "Utilisateur", "Batch"
    };

    public byte[] BuildLedgerWorkbook(AccountingLedgerExportModel model)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Brouillard Comptable");

        ws.Cell(1, 1).Value = "Brouillard Comptable";
        ws.Range(1, 1, 1, Headers.Length).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Date d'export : {model.ExportDate:dd/MM/yyyy HH:mm}";
        ws.Cell(3, 1).Value = $"Exporté par : {model.ExportedByName}";
        ws.Cell(4, 1).Value = $"Filtres utilisés : {model.FilterDescription}";
        for (var r = 2; r <= 4; r++) ws.Range(r, 1, r, Headers.Length).Merge();

        const int headerRow = 6;
        for (var i = 0; i < Headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = Headers[i];
        ws.Row(headerRow).Style.Font.Bold = true;
        ws.Row(headerRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
        ws.SheetView.Freeze(headerRow, 0);

        var row = headerRow + 1;
        decimal totalDebit = 0, totalCredit = 0;
        foreach (var r in model.Rows)
        {
            ws.Cell(row, 1).Value = r.EntryDate;
            ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 2).Value = r.OperationDate;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 3).Value = r.JournalCode;
            ws.Cell(row, 4).Value = r.Reference;
            ws.Cell(row, 5).Value = r.PieceNumber ?? string.Empty;
            ws.Cell(row, 6).Value = $"{r.AccountNumber} - {r.AccountName}";
            ws.Cell(row, 7).Value = r.Description;
            ws.Cell(row, 8).Value = r.Debit;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = r.Credit;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = r.UserName;
            ws.Cell(row, 11).Value = r.BatchReference;

            totalDebit += r.Debit;
            totalCredit += r.Credit;
            row++;
        }

        row += 1;
        ws.Cell(row, 1).Value = "Total Débit";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = totalDebit;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;
        ws.Cell(row, 1).Value = "Total Crédit";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = totalCredit;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
        row++;
        ws.Cell(row, 1).Value = "Différence";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = totalDebit - totalCredit;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
