using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(g => g.Fee).HasPrecision(10, 2);

        builder.HasOne(g => g.Template)
            .WithMany()
            .HasForeignKey(g => g.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(g => g.StartsAt);
    }
}
