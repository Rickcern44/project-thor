using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class RosterRecordConfiguration : IEntityTypeConfiguration<RosterRecord>
{
    public void Configure(EntityTypeBuilder<RosterRecord> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Email).HasMaxLength(320).IsRequired();
        builder.Property(r => r.Phone).HasMaxLength(32).IsRequired();
        builder.Property(r => r.LegacyBalance).HasPrecision(10, 2);

        builder.HasIndex(r => r.Email).IsUnique();
    }
}
