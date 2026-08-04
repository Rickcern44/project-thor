namespace ProjectThor.Api.Infrastructure.Import;

/// <summary>Serialized into FlaggedImportRow.RawData - everything needed to resolve the row later without re-parsing the CSV.</summary>
public sealed record PendingRosterRow(string Name, List<DateOnly> AttendedDates, decimal TotalDue, decimal AmountPaid);
