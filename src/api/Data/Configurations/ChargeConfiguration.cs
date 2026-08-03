using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.Property(c => c.Amount).HasPrecision(10, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasOne(c => c.Game)
            .WithMany()
            .HasForeignKey(c => c.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.PlayerUser)
            .WithMany()
            .HasForeignKey(c => c.PlayerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.PlayerUserId, c.Status });
    }
}
