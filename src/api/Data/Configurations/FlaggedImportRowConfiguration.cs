using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class FlaggedImportRowConfiguration : IEntityTypeConfiguration<FlaggedImportRow>
{
    public void Configure(EntityTypeBuilder<FlaggedImportRow> builder)
    {
        builder.Property(r => r.RawData).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasOne(r => r.ResolvedRosterRecord)
            .WithMany()
            .HasForeignKey(r => r.ResolvedRosterRecordId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
