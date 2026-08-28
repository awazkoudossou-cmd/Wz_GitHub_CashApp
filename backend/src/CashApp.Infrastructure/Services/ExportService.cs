using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Interfaces;
using CashApp.Application.Settings;
using CashApp.Domain.Constants;
using CashApp.Domain.Entities;
using CashApp.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CashApp.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly IAppDbContext _db;
    private readonly ISettingsService _settings;

    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ExportService(IAppDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<byte[]> ExportOperationsExcelAsync(DateTime from, DateTime to, int? cashRegisterId,
        string? direction, bool includeDeleted, CancellationToken ct = default)
    {
        var q = _db.CashOperations.AsNoTracking();
        if (includeDeleted) q = q.IgnoreQueryFilters(); // expose aussi les soft-deleted (annulées/rejetées)
        q = q.Include(o => o.CashRegister)
             .Include(o => o.CashSession)
             .Include(o => o.Category)
             .Where(o => o.OperationDate >= from && o.OperationDate <= to);
        if (cashRegisterId.HasValue) q = q.Where(o => o.CashRegisterId == cashRegisterId.Value);
        if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<OperationDirection>(direction, true, out var dir))
            q = q.Where(o => o.Direction == dir);

        var ops = await q.OrderBy(o => o.OperationDate).ThenBy(o => o.Id).ToListAsync(ct);

        // Résolution des noms d'utilisateurs (création / modification / suppression) en une seule requête.
        var userIds = ops.SelectMany(o => new[] { o.CreatedBy, o.UpdatedBy, o.DeletedBy })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var userNames = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        string UserName(int? id) => id.HasValue && userNames.TryGetValue(id.Value, out var n) ? n : string.Empty;

        using var wb = new XLWorkbook();

        // --- Palette / styles communs ---
        var brandBlue = XLColor.FromHtml("#1976D2");
        var green = XLColor.FromHtml("#2E7D32");
        var red = XLColor.FromHtml("#C62828");
        var lightGreen = XLColor.FromHtml("#E8F5E9");
        var lightRed = XLColor.FromHtml("#FFEBEE");
        var lightGray = XLColor.FromHtml("#F5F5F5");
        var headerGray = XLColor.FromHtml("#424242");

        BuildOperationsSummarySheet(wb, ops, from, to, brandBlue, green, red, lightGray, headerGray);
        BuildOperationsDetailSheet(wb, ops, UserName, brandBlue, lightGreen, lightRed, lightGray, green, red);

        wb.Worksheets.Worksheet("Résumé").SetTabActive();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void BuildOperationsDetailSheet(XLWorkbook wb, List<CashOperation> ops, Func<int?, string> userName,
        XLColor brandBlue, XLColor lightGreen, XLColor lightRed, XLColor lightGray, XLColor green, XLColor red)
    {
        var ws = wb.Worksheets.Add("Opérations");

        var headers = new[]
        {
            "Référence", "Date opération", "Code caisse", "Nom caisse", "Session #", "Direction",
            "Code catégorie", "Catégorie", "Montant", "Montant net", "Devise", "Moyen de paiement",
            "Libellé", "Description", "Tiers", "Référence externe",
            "En attente d'approbation", "En attente d'annulation", "Annulée",
            "Motif d'annulation", "Annulée par", "Annulée le",
            "Créée par", "Créée le", "Modifiée par", "Modifiée le"
        };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = brandBlue;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.SheetView.FreezeRows(1);
        ws.Row(1).Height = 20;

        int row = 2;
        foreach (var o in ops)
        {
            int c = 1;
            ws.Cell(row, c++).Value = o.OperationRef;
            ws.Cell(row, c).Value = o.OperationDate; ws.Cell(row, c++).Style.DateFormat.Format = "yyyy-MM-dd";
            ws.Cell(row, c++).Value = o.CashRegister.Code;
            ws.Cell(row, c++).Value = o.CashRegister.Name;
            ws.Cell(row, c++).Value = o.CashSessionId;
            var dirCell = ws.Cell(row, c++); dirCell.Value = o.Direction.ToString();
            ws.Cell(row, c++).Value = o.Category.Code;
            ws.Cell(row, c++).Value = o.Category.Label;
            var amountCell = ws.Cell(row, c);
            amountCell.Value = (double)o.Amount;
            amountCell.Style.NumberFormat.Format = "#,##0.00";
            c++;
            // Montant net : signé selon la direction (IN = +, OUT = -), cohérent avec le calcul
            // du solde théorique (totalIn - totalOut) — utile pour sommer directement la colonne.
            var netAmount = o.Direction == OperationDirection.IN ? o.Amount : -o.Amount;
            var netCell = ws.Cell(row, c);
            netCell.Value = (double)netAmount;
            netCell.Style.NumberFormat.Format = "#,##0.00";
            c++;
            ws.Cell(row, c++).Value = o.CurrencyCode;
            ws.Cell(row, c++).Value = o.PaymentMethod.ToString();
            ws.Cell(row, c++).Value = o.Label;
            ws.Cell(row, c++).Value = o.Description ?? string.Empty;
            ws.Cell(row, c++).Value = o.ThirdPartyName ?? string.Empty;
            ws.Cell(row, c++).Value = o.ExternalReference ?? string.Empty;
            var pendingApprovalCell = ws.Cell(row, c++); pendingApprovalCell.Value = o.IsPendingApproval ? "Oui" : "Non";
            var pendingCancelCell = ws.Cell(row, c++); pendingCancelCell.Value = o.IsPendingCancellation ? "Oui" : "Non";
            var deletedCell = ws.Cell(row, c++); deletedCell.Value = o.IsDeleted ? "Oui" : "Non";
            ws.Cell(row, c++).Value = o.DeleteReason ?? string.Empty;
            ws.Cell(row, c++).Value = userName(o.DeletedBy);
            if (o.DeletedAt.HasValue) { ws.Cell(row, c).Value = o.DeletedAt.Value; ws.Cell(row, c++).Style.DateFormat.Format = "yyyy-MM-dd HH:mm"; }
            else c++;
            ws.Cell(row, c++).Value = userName(o.CreatedBy);
            ws.Cell(row, c).Value = o.CreatedAt; ws.Cell(row, c++).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(row, c++).Value = userName(o.UpdatedBy);
            if (o.UpdatedAt.HasValue) { ws.Cell(row, c).Value = o.UpdatedAt.Value; ws.Cell(row, c++).Style.DateFormat.Format = "yyyy-MM-dd HH:mm"; }
            else c++;

            // Mise en forme conditionnelle "manuelle" (fiable sur tous les lecteurs Excel) :
            // ligne entière teintée légèrement en fonction de la direction, montant coloré,
            // rayures alternées sur les lignes neutres pour la lisibilité.
            var isIn = o.Direction == OperationDirection.IN;
            var rowRange = ws.Range(row, 1, row, headers.Length);
            if (o.IsDeleted)
            {
                rowRange.Style.Font.FontColor = XLColor.FromHtml("#9E9E9E");
                rowRange.Style.Font.Strikethrough = true;
            }
            else
            {
                rowRange.Style.Fill.BackgroundColor = isIn ? lightGreen : lightRed;
                if (row % 2 == 0) rowRange.Style.Fill.BackgroundColor = isIn
                    ? XLColor.FromHtml("#E1F3E3") : XLColor.FromHtml("#FBE4E7"); // alternance légère
            }
            amountCell.Style.Font.FontColor = isIn ? green : red;
            amountCell.Style.Font.Bold = true;
            netCell.Style.Font.FontColor = netAmount >= 0 ? green : red;
            netCell.Style.Font.Bold = true;
            if (o.IsPendingApproval || o.IsPendingCancellation)
            {
                pendingApprovalCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
                pendingCancelCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
            }
            if (o.IsDeleted) deletedCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE");

            row++;
        }

        if (ops.Count > 0)
        {
            var dataRange = ws.Range(1, 1, row - 1, headers.Length);
            dataRange.SetAutoFilter();
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#BDBDBD");
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E0E0E0");
        }

        ws.Columns().AdjustToContents();
        ws.Column(14).Width = Math.Min(ws.Column(14).Width, 40); // Description : limite la largeur
    }

    private static void BuildOperationsSummarySheet(XLWorkbook wb, List<CashOperation> ops, DateTime from, DateTime to,
        XLColor brandBlue, XLColor green, XLColor red, XLColor lightGray, XLColor headerGray)
    {
        var ws = wb.Worksheets.Add("Résumé");
        ws.ShowGridLines = false;
        for (int col = 1; col <= 12; col++) ws.Column(col).Width = 12;

        // --- Titre ---
        var title = ws.Range("A1:L1");
        title.Merge();
        title.Value = "Résumé — Journal des opérations";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 18;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = brandBlue;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 32;

        var subtitle = ws.Range("A2:L2");
        subtitle.Merge();
        subtitle.Value = $"Période : {from:dd/MM/yyyy} → {to:dd/MM/yyyy}   •   Généré le {DateTime.Now:dd/MM/yyyy HH:mm}   •   {ops.Count} opération(s)";
        subtitle.Style.Font.Italic = true;
        subtitle.Style.Font.FontColor = XLColor.FromHtml("#616161");

        // --- Agrégats ---
        var active = ops.Where(o => !o.IsDeleted).ToList();
        var totalIn = active.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = active.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        var net = totalIn - totalOut;
        var nbPendingApproval = active.Count(o => o.IsPendingApproval);
        var nbPendingCancel = active.Count(o => o.IsPendingCancellation);
        var nbDeleted = ops.Count(o => o.IsDeleted);
        var currency = ops.FirstOrDefault()?.CurrencyCode ?? "XOF";

        // --- Cartes KPI (ligne 4-6) ---
        void Kpi(string startCell, string endCell, string label, string value, XLColor color)
        {
            var card = ws.Range($"{startCell}:{endCell}");
            card.Merge();
            card.Style.Fill.BackgroundColor = color;
            card.Style.Font.FontColor = XLColor.White;
            var lbl = ws.Cell(startCell);
            // On simule 2 lignes (libellé + valeur) via un retour à la ligne dans la cellule fusionnée.
            lbl.Value = $"{label}\n{value}";
            lbl.Style.Alignment.WrapText = true;
            lbl.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            lbl.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            lbl.Style.Font.Bold = true;
            lbl.Style.Font.FontSize = 13;
        }
        ws.Row(4).Height = 42;
        Kpi("A4", "C4", "Total Entrées", $"{totalIn:N0} {currency}", green);
        Kpi("D4", "F4", "Total Sorties", $"{totalOut:N0} {currency}", red);
        Kpi("G4", "I4", "Solde net", $"{net:N0} {currency}", brandBlue);
        Kpi("J4", "L4", "Nb opérations", active.Count.ToString(), headerGray);

        // --- Indicateurs secondaires (ligne 6) ---
        ws.Cell("A6").Value = "En attente d'approbation :"; ws.Cell("B6").Value = nbPendingApproval;
        ws.Cell("D6").Value = "En attente d'annulation :"; ws.Cell("E6").Value = nbPendingCancel;
        ws.Cell("G6").Value = "Annulées / rejetées :"; ws.Cell("H6").Value = nbDeleted;
        ws.Range("A6:H6").Style.Font.FontColor = XLColor.FromHtml("#424242");
        ws.Range("A6:H6").Style.Font.Bold = true;

        // --- Tableau : répartition par caisse ---
        var byRegister = active.GroupBy(o => o.CashRegister.Code)
            .Select(g => (Label: g.Key, Net: (double)(g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount)
                                                        - g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount)),
                          Count: g.Count()))
            .OrderByDescending(x => Math.Abs(x.Net)).ToList();

        int r = 8;
        ws.Cell(r, 1).Value = "Répartition par caisse"; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 1).Style.Font.FontSize = 13;
        r++;
        var t1Header = ws.Range(r, 1, r, 3);
        ws.Cell(r, 1).Value = "Caisse"; ws.Cell(r, 2).Value = "Nb ops"; ws.Cell(r, 3).Value = "Net";
        t1Header.Style.Font.Bold = true; t1Header.Style.Fill.BackgroundColor = lightGray;
        r++;
        var t1Start = r;
        foreach (var g in byRegister)
        {
            ws.Cell(r, 1).Value = g.Label;
            ws.Cell(r, 2).Value = g.Count;
            ws.Cell(r, 3).Value = g.Net; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 3).Style.Font.FontColor = g.Net >= 0 ? green : red;
            r++;
        }
        if (r > t1Start) ws.Range(t1Start, 1, r - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        var byRegisterTableEnd = r;

        // --- Tableau : répartition par catégorie (top 10, par montant net absolu) ---
        var byCategory = active.GroupBy(o => o.Category.Label)
            .Select(g => (Label: g.Key, Net: (double)(g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount)
                                                        - g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount)),
                          Count: g.Count()))
            .OrderByDescending(x => Math.Abs(x.Net)).Take(10).ToList();

        r = byRegisterTableEnd + 2;
        ws.Cell(r, 1).Value = "Répartition par catégorie (top 10)"; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 1).Style.Font.FontSize = 13;
        r++;
        var t2Header = ws.Range(r, 1, r, 3);
        ws.Cell(r, 1).Value = "Catégorie"; ws.Cell(r, 2).Value = "Nb ops"; ws.Cell(r, 3).Value = "Net";
        t2Header.Style.Font.Bold = true; t2Header.Style.Fill.BackgroundColor = lightGray;
        r++;
        var t2Start = r;
        foreach (var g in byCategory)
        {
            ws.Cell(r, 1).Value = g.Label;
            ws.Cell(r, 2).Value = g.Count;
            ws.Cell(r, 3).Value = g.Net; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 3).Style.Font.FontColor = g.Net >= 0 ? green : red;
            r++;
        }
        if (r > t2Start) ws.Range(t2Start, 1, r - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // --- Tableau : répartition par mois (évolution) ---
        var byMonth = active.GroupBy(o => o.OperationDate.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => (Label: g.Key, Net: (double)(g.Where(x => x.Direction == OperationDirection.IN).Sum(x => x.Amount)
                                                        - g.Where(x => x.Direction == OperationDirection.OUT).Sum(x => x.Amount)),
                          Count: g.Count()))
            .ToList();

        r = byRegisterTableEnd > 0 ? r + 2 : r + 2; // séparation après le tableau catégories
        ws.Cell(r, 1).Value = "Répartition par mois"; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 1).Style.Font.FontSize = 13;
        r++;
        var t3Header = ws.Range(r, 1, r, 3);
        ws.Cell(r, 1).Value = "Mois"; ws.Cell(r, 2).Value = "Nb ops"; ws.Cell(r, 3).Value = "Net";
        t3Header.Style.Font.Bold = true; t3Header.Style.Fill.BackgroundColor = lightGray;
        r++;
        var t3Start = r;
        foreach (var g in byMonth)
        {
            ws.Cell(r, 1).Value = g.Label;
            ws.Cell(r, 2).Value = g.Count;
            ws.Cell(r, 3).Value = g.Net; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 3).Style.Font.FontColor = g.Net >= 0 ? green : red;
            r++;
        }
        if (r > t3Start) ws.Range(t3Start, 1, r - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        ws.Columns(1, 3).AdjustToContents();
    }

    public async Task<byte[]> ExportOperationsPdfAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default)
    {
        var ops = await LoadOperationsAsync(from, to, cashRegisterId, ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(PdfFonts.Inter));

                page.Header().Column(col =>
                {
                    col.Item().Text("Journal des opérations").FontSize(14).Bold();
                    col.Item().Text($"Période : {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn();
                        c.RelativeColumn(); c.RelativeColumn(2); c.RelativeColumn();
                        c.RelativeColumn(3);
                    });
                    table.Header(h =>
                    {
                        foreach (var col in new[] { "Référence", "Date", "Caisse", "Dir.", "Catégorie", "Montant", "Libellé" })
                            h.Cell().BorderBottom(1).Padding(2).Text(col).Bold();
                    });
                    foreach (var o in ops)
                    {
                        table.Cell().Padding(2).Text(o.OperationRef);
                        table.Cell().Padding(2).Text(o.OperationDate.ToString("yyyy-MM-dd"));
                        table.Cell().Padding(2).Text(o.CashRegister.Code);
                        table.Cell().Padding(2).Text(o.Direction.ToString());
                        table.Cell().Padding(2).Text(o.Category.Label);
                        table.Cell().Padding(2).AlignRight().Text(o.Amount.ToString("N2"));
                        table.Cell().Padding(2).Text(o.Label);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber(); x.Span(" / "); x.TotalPages();
                });
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportSessionsExcelAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default)
    {
        var sessions = await LoadSessionsAsync(from, to, cashRegisterId, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sessions");

        var headers = new[] { "ID", "Caisse", "Ouverte par", "Ouverte le", "Solde ouv.", "Fermée le", "Solde théo.", "Solde phy.", "Écart", "Statut" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var s in sessions)
        {
            ws.Cell(row, 1).Value = s.Id;
            ws.Cell(row, 2).Value = s.CashRegister.Code;
            ws.Cell(row, 3).Value = s.OpenedByUser.FullName;
            ws.Cell(row, 4).Value = s.OpenedAt;
            ws.Cell(row, 4).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(row, 5).Value = (double)s.OpeningBalance;
            ws.Cell(row, 6).Value = s.ClosedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
            ws.Cell(row, 7).Value = s.TheoreticalBalance.HasValue ? (double)s.TheoreticalBalance.Value : 0;
            ws.Cell(row, 8).Value = s.PhysicalBalance.HasValue ? (double)s.PhysicalBalance.Value : 0;
            ws.Cell(row, 9).Value = s.VarianceAmount.HasValue ? (double)s.VarianceAmount.Value : 0;
            ws.Cell(row, 10).Value = s.Status.ToString();
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAuditLogsExcelAsync(string? actionType, string? entityType, int? entityId,
        int? performedBy, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking().Include(a => a.PerformedByUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(actionType) && Enum.TryParse<AuditAction>(actionType, true, out var action))
            q = q.Where(a => a.ActionType == action);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (entityId.HasValue) q = q.Where(a => a.EntityId == entityId.Value);
        if (performedBy.HasValue) q = q.Where(a => a.PerformedBy == performedBy.Value);
        if (from.HasValue) q = q.Where(a => a.PerformedAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.PerformedAt <= to.Value);

        // Garde-fou export : jamais plus de 20 000 lignes en une fois.
        var logs = await q.OrderByDescending(a => a.PerformedAt).Take(20_000).ToListAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Journal d'audit");

        var headers = new[] { "Date", "Action", "Entité", "ID entité", "Utilisateur", "Description" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var a in logs)
        {
            ws.Cell(row, 1).Value = a.PerformedAt;
            ws.Cell(row, 1).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(row, 2).Value = a.ActionType.ToString();
            ws.Cell(row, 3).Value = a.EntityType;
            ws.Cell(row, 4).Value = a.EntityId?.ToString() ?? string.Empty;
            ws.Cell(row, 5).Value = a.PerformedByUser?.FullName ?? string.Empty;
            ws.Cell(row, 6).Value = a.Description ?? string.Empty;
            row++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportSessionsPdfAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct = default)
    {
        var sessions = await LoadSessionsAsync(from, to, cashRegisterId, ct);
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(PdfFonts.Inter));
                page.Header().Text("Sessions de caisse").FontSize(14).Bold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        c.RelativeColumn(); c.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        foreach (var col in new[] { "ID", "Caisse", "Ouverte par", "Ouverte le", "Solde ouv.", "Théorique", "Physique", "Écart" })
                            h.Cell().BorderBottom(1).Padding(2).Text(col).Bold();
                    });
                    foreach (var s in sessions)
                    {
                        table.Cell().Padding(2).Text(s.Id.ToString());
                        table.Cell().Padding(2).Text(s.CashRegister.Code);
                        table.Cell().Padding(2).Text(s.OpenedByUser.FullName);
                        table.Cell().Padding(2).Text(s.OpenedAt.ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Padding(2).AlignRight().Text(s.OpeningBalance.ToString("N2"));
                        table.Cell().Padding(2).AlignRight().Text(s.TheoreticalBalance?.ToString("N2") ?? "-");
                        table.Cell().Padding(2).AlignRight().Text(s.PhysicalBalance?.ToString("N2") ?? "-");
                        table.Cell().Padding(2).AlignRight().Text(s.VarianceAmount?.ToString("N2") ?? "-");
                    }
                });
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportCashStatePdfAsync(int cashSessionId, CancellationToken ct = default)
    {
        var session = await _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .Include(s => s.ClosedByUser)
            .FirstOrDefaultAsync(s => s.Id == cashSessionId, ct)
            ?? throw new NotFoundException(nameof(CashSession), cashSessionId);

        var ops = await _db.CashOperations.AsNoTracking()
            .Include(o => o.Category)
            .Where(o => o.CashSessionId == cashSessionId && !o.IsDeleted)
            .OrderBy(o => o.OperationDate).ThenBy(o => o.Id)
            .ToListAsync(ct);

        var company = await _settings.GetCompanyAsync(ct);
        var totalIn = ops.Where(o => o.Direction == OperationDirection.IN).Sum(o => o.Amount);
        var totalOut = ops.Where(o => o.Direction == OperationDirection.OUT).Sum(o => o.Amount);
        var theoretical = session.OpeningBalance + totalIn - totalOut;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(PdfFonts.Inter));

                page.Header().Column(col =>
                {
                    // En-tête entreprise.
                    col.Item().AlignCenter().Text(company.Name ?? "Votre entreprise").Bold().FontSize(13);
                    if (!string.IsNullOrWhiteSpace(company.LegalForm))
                        col.Item().AlignCenter().Text(company.LegalForm).Italic().FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        col.Item().AlignCenter().Text(company.Address).FontSize(9);
                    var loc = string.Join(" — ", new[] { company.City, company.Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(loc)) col.Item().AlignCenter().Text(loc).FontSize(9);
                    var contact = string.Join(" • ", new[] { company.Phone, company.Email, company.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(contact)) col.Item().AlignCenter().Text(contact).FontSize(9);
                    var legal = string.Join(" • ", new[]
                        {
                            !string.IsNullOrWhiteSpace(company.RegistrationNumber) ? $"RCCM/SIRET {company.RegistrationNumber}" : null,
                            !string.IsNullOrWhiteSpace(company.TaxId) ? $"NIF/TVA {company.TaxId}" : null
                        }.Where(s => s is not null));
                    if (!string.IsNullOrWhiteSpace(legal)) col.Item().AlignCenter().Text(legal).Italic().FontSize(8);

                    col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    // Titre du document.
                    col.Item().Text($"État de caisse — {session.CashRegister.Name}").FontSize(14).Bold();
                    col.Item().Text($"Session #{session.Id} — ouvert par {session.OpenedByUser.FullName} le {session.OpenedAt:yyyy-MM-dd HH:mm}");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Solde d'ouverture : {session.OpeningBalance:N2}");
                        r.RelativeItem().Text($"Entrées : {totalIn:N2}");
                        r.RelativeItem().Text($"Sorties : {totalOut:N2}");
                        r.RelativeItem().Text($"Théorique : {theoretical:N2}");
                    });

                    if (session.PhysicalBalance.HasValue)
                    {
                        col.Item().Text($"Solde physique : {session.PhysicalBalance:N2}");
                        col.Item().Text($"Écart : {session.VarianceAmount:N2}").Bold();
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(); c.RelativeColumn();
                            c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Date", "Dir.", "Catégorie", "Libellé", "Montant" })
                                h.Cell().BorderBottom(1).Padding(2).Text(t).Bold();
                        });
                        foreach (var o in ops)
                        {
                            table.Cell().Padding(2).Text(o.OperationDate.ToString("yyyy-MM-dd"));
                            table.Cell().Padding(2).Text(o.Direction.ToString());
                            table.Cell().Padding(2).Text(o.Category.Label);
                            table.Cell().Padding(2).Text(o.Label);
                            table.Cell().Padding(2).AlignRight().Text(o.Amount.ToString("N2"));
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportOperationReceiptPdfAsync(int operationId, CancellationToken ct = default)
    {
        var op = await _db.CashOperations.AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.CashRegister)
            .Include(o => o.CashSession)
            .Include(o => o.Category)
            .FirstOrDefaultAsync(o => o.Id == operationId, ct)
            ?? throw new NotFoundException(nameof(CashOperation), operationId);

        var company = await _settings.GetCompanyAsync(ct);
        var copiesRaw = await _settings.GetRawAsync(SettingKeys.ReceiptCopiesCount, ct);
        var copies = int.TryParse(copiesRaw, out var c) && c == 2 ? 2 : 1;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(12);
                // Police adaptée : plus petite si 2 copies sur la même page.
                var baseFontSize = copies == 2 ? 7.5f : 10f;
                page.DefaultTextStyle(x => x.FontSize(baseFontSize).FontFamily(PdfFonts.Inter));

                page.Content().Column(col =>
                {
                    void DrawReceipt(QuestPDF.Infrastructure.IContainer container, string copyLabel)
                    {
                        container.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(receipt =>
                        {
                            // --- En-tête entreprise ---
                            receipt.Item().AlignCenter().Text(company.Name ?? "Votre entreprise")
                                .Bold().FontSize(baseFontSize + 3);
                            if (!string.IsNullOrWhiteSpace(company.LegalForm))
                                receipt.Item().AlignCenter().Text(company.LegalForm).Italic();
                            if (!string.IsNullOrWhiteSpace(company.Address))
                                receipt.Item().AlignCenter().Text(company.Address);
                            var line2 = string.Join(" — ", new[] { company.City, company.Country }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (!string.IsNullOrWhiteSpace(line2)) receipt.Item().AlignCenter().Text(line2);
                            var contact = string.Join(" • ", new[] { company.Phone, company.Email, company.Website }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (!string.IsNullOrWhiteSpace(contact)) receipt.Item().AlignCenter().Text(contact);
                            var legal = string.Join(" • ", new[]
                                {
                                    !string.IsNullOrWhiteSpace(company.RegistrationNumber) ? $"RCCM/SIRET {company.RegistrationNumber}" : null,
                                    !string.IsNullOrWhiteSpace(company.TaxId) ? $"NIF/TVA {company.TaxId}" : null
                                }.Where(s => s is not null));
                            if (!string.IsNullOrWhiteSpace(legal)) receipt.Item().AlignCenter().Text(legal).FontSize(baseFontSize - 1).Italic();

                            receipt.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                            // --- Titre du reçu --- (suffixe DRAFT si en attente d'approbation)
                            var title = op.Direction == OperationDirection.IN ? "REÇU D'ENCAISSEMENT" : "REÇU DE PAIEMENT";
                            if (op.IsPendingApproval) title = $"{title}  ## DRAFT ##";
                            receipt.Item().AlignCenter().Text(title)
                                .Bold().FontSize(baseFontSize + 2);
                            if (!string.IsNullOrWhiteSpace(copyLabel))
                                receipt.Item().AlignCenter().Text(copyLabel).Italic().FontSize(baseFontSize - 1);

                            receipt.Item().PaddingTop(4);

                            // --- Détail ---
                            receipt.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Référence : ").SemiBold(); t.Span(op.OperationRef); });
                                r.RelativeItem().AlignRight().Text(t => { t.Span("Date : ").SemiBold(); t.Span(op.OperationDate.ToString("yyyy-MM-dd")); });
                            });
                            receipt.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Caisse : ").SemiBold(); t.Span($"{op.CashRegister.Code} — {op.CashRegister.Name}"); });
                                r.RelativeItem().AlignRight().Text(t => { t.Span("Session : ").SemiBold(); t.Span($"#{op.CashSessionId}"); });
                            });
                            receipt.Item().Text(t => { t.Span("Catégorie : ").SemiBold(); t.Span(op.Category.Label); });
                            receipt.Item().Text(t => { t.Span("Moyen de paiement : ").SemiBold(); t.Span(op.PaymentMethod.ToString()); });
                            if (!string.IsNullOrWhiteSpace(op.ThirdPartyName))
                                receipt.Item().Text(t => { t.Span("Tiers : ").SemiBold(); t.Span(op.ThirdPartyName); });

                            receipt.Item().PaddingTop(4).Text(t => { t.Span("Libellé : ").SemiBold(); t.Span(op.Label); });
                            if (!string.IsNullOrWhiteSpace(op.Description))
                                receipt.Item().Text(t => { t.Span("Description : ").SemiBold(); t.Span(op.Description); });

                            receipt.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                            // --- Montant en évidence ---
                            receipt.Item().AlignCenter().Text(t =>
                            {
                                t.Span("MONTANT : ").Bold().FontSize(baseFontSize + 3);
                                t.Span($"{op.Amount:N2} {op.CurrencyCode}").Bold().FontSize(baseFontSize + 5);
                            });

                            receipt.Item().PaddingTop(8);

                            // --- Signatures ---
                            receipt.Item().Row(r =>
                            {
                                r.RelativeItem().AlignCenter().Text("Signature & cachet").FontSize(baseFontSize - 1);
                                r.RelativeItem().AlignCenter().Text("Bénéficiaire").FontSize(baseFontSize - 1);
                            });
                            receipt.Item().Row(r =>
                            {
                                r.RelativeItem().Height(28).BorderTop(0.5f).BorderColor(Colors.Grey.Medium);
                                r.RelativeItem().Height(28).BorderTop(0.5f).BorderColor(Colors.Grey.Medium);
                            });

                            if (op.IsDeleted)
                                receipt.Item().AlignCenter().Text("OPÉRATION ANNULÉE").Bold().FontColor(Colors.Red.Medium);
                        });
                    }

                    if (copies == 2)
                    {
                        col.Item().Element(c => DrawReceipt(c, "(Copie originale)"));
                        col.Item().PaddingVertical(4);
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                        col.Item().PaddingVertical(4);
                        col.Item().Element(c => DrawReceipt(c, "(Copie souche)"));
                    }
                    else
                    {
                        col.Item().Element(c => DrawReceipt(c, ""));
                    }
                });
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportApprovalRequestPdfAsync(int approvalRequestId, CancellationToken ct = default)
    {
        var req = await _db.ApprovalRequests.AsNoTracking()
            .Include(r => r.ApprovalRule)
            .Include(r => r.CashRegister)
            .Include(r => r.RequestedByUser)
            .Include(r => r.DecidedByUser)
            .Include(r => r.Actions).ThenInclude(a => a.PerformedByUser)
            .FirstOrDefaultAsync(r => r.Id == approvalRequestId, ct)
            ?? throw new NotFoundException(nameof(CashApp.Domain.Entities.V2.ApprovalRequest), approvalRequestId);

        var company = await _settings.GetCompanyAsync(ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(PdfFonts.Inter));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(company.Name ?? "Votre entreprise").Bold().FontSize(13);
                    if (!string.IsNullOrWhiteSpace(company.LegalForm))
                        col.Item().AlignCenter().Text(company.LegalForm).Italic().FontSize(9);
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        col.Item().AlignCenter().Text(company.Address).FontSize(9);
                    var loc = string.Join(" — ", new[] { company.City, company.Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(loc)) col.Item().AlignCenter().Text(loc).FontSize(9);
                    var contact = string.Join(" • ", new[] { company.Phone, company.Email, company.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(contact)) col.Item().AlignCenter().Text(contact).FontSize(9);
                    col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                    col.Item().Text($"Demande d'approbation {req.RequestRef}").FontSize(14).Bold();
                    col.Item().Text($"Statut : {req.Status} — Cible : {req.TargetType} #{req.TargetEntityId}").FontSize(10);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Règle : ").SemiBold(); t.Span($"{req.ApprovalRule.Code} — {req.ApprovalRule.Name}"); });
                        r.RelativeItem().AlignRight().Text(t => { t.Span("Demandée le : ").SemiBold(); t.Span(req.RequestedAt.ToString("yyyy-MM-dd HH:mm")); });
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Demandée par : ").SemiBold(); t.Span(req.RequestedByUser.FullName); });
                        r.RelativeItem().AlignRight().Text(t => { t.Span("Caisse : ").SemiBold(); t.Span(req.CashRegister?.Code ?? "—"); });
                    });
                    if (req.Amount.HasValue)
                    {
                        col.Item().Text(t => { t.Span("Montant : ").SemiBold(); t.Span($"{req.Amount.Value:N2} {req.CurrencyCode}"); });
                    }
                    col.Item().Text(t => { t.Span("Motif : ").SemiBold(); t.Span(req.Reason); });

                    if (req.DecidedAt.HasValue)
                    {
                        col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().Text("Décision").Bold().FontSize(11);
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t => { t.Span("Décidée par : ").SemiBold(); t.Span(req.DecidedByUser?.FullName ?? "—"); });
                            r.RelativeItem().AlignRight().Text(t => { t.Span("Décidée le : ").SemiBold(); t.Span(req.DecidedAt.Value.ToString("yyyy-MM-dd HH:mm")); });
                        });
                        if (!string.IsNullOrWhiteSpace(req.DecisionComment))
                            col.Item().Text(t => { t.Span("Commentaire : ").SemiBold(); t.Span(req.DecisionComment); });
                    }

                    if (req.Actions.Count > 0)
                    {
                        col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                        col.Item().Text("Historique des actions").Bold().FontSize(11);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(3); });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "Date", "Action", "Par", "Commentaire" })
                                    h.Cell().BorderBottom(1).Padding(2).Text(t).Bold();
                            });
                            foreach (var a in req.Actions.OrderBy(a => a.PerformedAt))
                            {
                                table.Cell().Padding(2).Text(a.PerformedAt.ToString("yyyy-MM-dd HH:mm"));
                                table.Cell().Padding(2).Text(a.ActionType.ToString());
                                table.Cell().Padding(2).Text(a.PerformedByUser?.FullName ?? "—");
                                table.Cell().Padding(2).Text(a.Comment ?? "");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf();

        return bytes;
    }

    private async Task<List<CashOperation>> LoadOperationsAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct)
    {
        var q = _db.CashOperations.AsNoTracking()
            .Include(o => o.CashRegister)
            .Include(o => o.Category)
            .Where(o => o.OperationDate >= from && o.OperationDate <= to);
        if (cashRegisterId.HasValue) q = q.Where(o => o.CashRegisterId == cashRegisterId.Value);
        return await q.OrderBy(o => o.OperationDate).ThenBy(o => o.Id).ToListAsync(ct);
    }

    private async Task<List<CashSession>> LoadSessionsAsync(DateTime from, DateTime to, int? cashRegisterId, CancellationToken ct)
    {
        var q = _db.CashSessions.AsNoTracking()
            .Include(s => s.CashRegister)
            .Include(s => s.OpenedByUser)
            .Where(s => s.OpenedAt >= from && s.OpenedAt <= to);
        if (cashRegisterId.HasValue) q = q.Where(s => s.CashRegisterId == cashRegisterId.Value);
        return await q.OrderBy(s => s.OpenedAt).ToListAsync(ct);
    }
}
