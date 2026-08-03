using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class GameTemplateConfiguration : IEntityTypeConfiguration<GameTemplate>
{
    public void Configure(EntityTypeBuilder<GameTemplate> builder)
    {
        builder.Property(t => t.Fee).HasPrecision(10, 2);
    }
}
