namespace ProjectThor.Data.Entities;

/// <summary>
/// A player record created by the one-time spreadsheet import. LegacyBalance captures
/// whatever was owed in the spreadsheet at import time; balance going forward is computed
/// from Charges once the record is linked to an activated User.
/// </summary>
public class RosterRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public decimal LegacyBalance { get; set; }
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;
}
