using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Features.GameTemplates;

public sealed record GameTemplateResponse(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly TimeOfDay,
    int DefaultCapacity,
    decimal Fee,
    TimeSpan SignupLeadTime,
    bool IsActive)
{
    public static GameTemplateResponse From(GameTemplate template) => new(
        template.Id,
        template.DayOfWeek,
        template.TimeOfDay,
        template.DefaultCapacity,
        template.Fee,
        template.SignupLeadTime,
        template.IsActive);
}
