using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class SignUpConfiguration : IEntityTypeConfiguration<SignUp>
{
    public void Configure(EntityTypeBuilder<SignUp> builder)
    {
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasOne(s => s.Game)
            .WithMany()
            .HasForeignKey(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.PlayerUser)
            .WithMany()
            .HasForeignKey(s => s.PlayerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A player has at most one active (non-cancelled) sign-up per game.
        builder.HasIndex(s => new { s.GameId, s.PlayerUserId })
            .IsUnique()
            .HasFilter("\"CancelledAt\" IS NULL");
    }
}
