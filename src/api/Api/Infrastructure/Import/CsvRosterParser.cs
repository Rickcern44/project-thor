using System.Globalization;

namespace ProjectThor.Api.Infrastructure.Import;

public sealed record ParsedRosterRow(
    string Name,
    IReadOnlyList<DateOnly> AttendedDates,
    decimal TotalDue,
    decimal AmountPaid,
    string? AttendanceMismatchNote);

/// <summary>
/// Parses the league's specific dues-tracking CSV shape: Name, then one column per weekly game
/// date ("8-Jan" style, no year - the year is supplied by the admin at upload time since the
/// sheet never states it), then Attendance/Total Due/Amount Paid/Balance summary columns. The
/// sheet's own Balance column is never trusted (see design notes) - callers compute it from
/// TotalDue/AmountPaid. Unlabeled trailing columns are ignored.
/// </summary>
public static class CsvRosterParser
{
    public static IReadOnlyList<ParsedRosterRow> Parse(string csvContent, int seasonYear)
    {
        var lines = csvContent
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return [];
        }

        var header = SplitLine(lines[0]);
        var dateColumns = new List<(int Index, DateOnly Date)>();
        var totalDueIndex = -1;
        var amountPaidIndex = -1;

        for (var i = 1; i < header.Count; i++)
        {
            var column = header[i].Trim();
            if (column.StartsWith("Total Due", StringComparison.OrdinalIgnoreCase))
            {
                totalDueIndex = i;
            }
            else if (column.Equals("Amount Paid", StringComparison.OrdinalIgnoreCase))
            {
                amountPaidIndex = i;
            }
            else if (TryParseDateColumn(column, seasonYear, out var date))
            {
                dateColumns.Add((i, date));
            }
            // "Attendance", "Balance", and any unlabeled trailing columns are intentionally ignored.
        }

        var rows = new List<ParsedRosterRow>();
        for (var r = 1; r < lines.Count; r++)
        {
            var cells = SplitLine(lines[r]);
            if (cells.Count == 0 || string.IsNullOrWhiteSpace(cells[0]))
            {
                continue;
            }

            var name = cells[0].Trim();
            var attendedDates = dateColumns
                .Where(dc => dc.Index < cells.Count && cells[dc.Index].Trim().Equals("x", StringComparison.OrdinalIgnoreCase))
                .Select(dc => dc.Date)
                .ToList();

            var totalDue = totalDueIndex >= 0 && totalDueIndex < cells.Count ? ParseCurrency(cells[totalDueIndex]) : 0m;
            var amountPaid = amountPaidIndex >= 0 && amountPaidIndex < cells.Count ? ParseCurrency(cells[amountPaidIndex]) : 0m;

            rows.Add(new ParsedRosterRow(name, attendedDates, totalDue, amountPaid, AttendanceMismatchNote: null));
        }

        return rows;
    }

    private static bool TryParseDateColumn(string header, int year, out DateOnly date) =>
        DateOnly.TryParseExact($"{header}-{year}", "d-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static decimal ParseCurrency(string cell)
    {
        var trimmed = cell.Trim();
        var isNegative = trimmed.StartsWith('(') && trimmed.EndsWith(')');
        var cleaned = trimmed.Trim('(', ')', '$', ' ').Replace(",", "");
        if (cleaned.Length == 0 || cleaned == "-")
        {
            return 0m;
        }

        var value = decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
        return isNegative ? -value : value;
    }

    private static List<string> SplitLine(string line) => [.. line.Split(',')];
}
