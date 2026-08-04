namespace ProjectThor.Data.Entities;

/// <summary>
/// A generator, not a live link to its instances (design.md D7) — editing the template
/// only affects games materialized after the edit.
/// </summary>
public class GameTemplate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly TimeOfDay { get; set; }
    public int DefaultCapacity { get; set; }
    public decimal Fee { get; set; }
    public TimeSpan SignupLeadTime { get; set; } = TimeSpan.FromDays(1);
    public bool IsActive { get; set; } = true;
}
