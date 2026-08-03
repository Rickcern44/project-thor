namespace ProjectThor.Data.Entities;

public enum ImportRowStatus
{
    Pending,
    Resolved
}

/// <summary>
/// A spreadsheet row the importer could not unambiguously turn into a RosterRecord
/// (missing required field, ambiguous match, malformed data). Held for admin review
/// rather than silently dropped or guessed.
/// </summary>
public class FlaggedImportRow
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string RawData { get; set; }
    public required string Reason { get; set; }
    public ImportRowStatus Status { get; set; } = ImportRowStatus.Pending;
    public Guid? ResolvedRosterRecordId { get; set; }
    public RosterRecord? ResolvedRosterRecord { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
