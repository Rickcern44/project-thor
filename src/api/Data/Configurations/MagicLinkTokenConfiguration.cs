using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class MagicLinkTokenConfiguration : IEntityTypeConfiguration<MagicLinkToken>
{
    public void Configure(EntityTypeBuilder<MagicLinkToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
