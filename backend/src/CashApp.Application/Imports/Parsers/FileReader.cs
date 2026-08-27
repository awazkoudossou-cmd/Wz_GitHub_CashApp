using System.Globalization;
using ClosedXML.Excel;

namespace CashApp.Application.Imports.Parsers;

// Utilitaires partagés CSV/Excel.
public static class FileReader
{
    public static async IAsyncEnumerable<ParsedRawRow> ReadAnyAsync(Stream stream, string fileExtension)
    {
        fileExtension = fileExtension.TrimStart('.').ToLowerInvariant();
        if (fileExtension == "csv")
        {
            await foreach (var row in ReadCsvAsync(stream)) yield return row;
        }
        else if (fileExtension is "xlsx" or "xls")
        {
            foreach (var row in ReadXlsx(stream)) yield return row;
            await Task.CompletedTask;
        }
        else
        {
            throw new InvalidOperationException($"Extension '{fileExtension}' non supportée pour l'import.");
        }
    }

    private static async IAsyncEnumerable<ParsedRawRow> ReadCsvAsync(Stream stream)
    {
        using var sr = new StreamReader(stream);
        string? headerLine = await sr.ReadLineAsync();
        if (headerLine is null) yield break;

        var headers = SplitCsv(headerLine);
        int lineNumber = 1;

        string? line;
        while ((line = await sr.ReadLineAsync()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = SplitCsv(line);
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                dict[headers[i]] = i < values.Length ? values[i] : null;
            }
            yield return new ParsedRawRow(lineNumber, dict);
        }
    }

    private static IEnumerable<ParsedRawRow> ReadXlsx(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var headerRow = ws.RowsUsed().First();
        var headers = headerRow.CellsUsed().Select(c => c.GetString().Trim()).ToArray();
        int lineNumber = 1;
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            lineNumber++;
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                var col = i + 1;
                var cell = row.Cell(col);
                dict[headers[i]] = cell.IsEmpty() ? null : cell.GetString();
            }
            yield return new ParsedRawRow(lineNumber, dict);
        }
    }

    public static string[] SplitCsv(string line)
    {
        // CSV minimal : virgule ou point-virgule, avec support quotes simples.
        var sep = line.Contains(';') ? ';' : ',';
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == sep && !inQuotes) { result.Add(sb.ToString().Trim()); sb.Clear(); continue; }
            sb.Append(c);
        }
        result.Add(sb.ToString().Trim());
        return result.ToArray();
    }

    public static decimal ParseDecimal(string raw, string decimalSep)
    {
        var s = raw.Replace(" ", "").Trim();
        if (decimalSep == ",") s = s.Replace(".", "").Replace(",", ".");
        else s = s.Replace(",", "");
        return decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public static DateTime ParseDate(string raw, string format)
    {
        if (DateTime.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;
        return DateTime.Parse(raw, CultureInfo.InvariantCulture);
    }
}
