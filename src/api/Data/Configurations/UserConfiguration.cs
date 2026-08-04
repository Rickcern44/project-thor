using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.RosterRecord)
            .WithMany()
            .HasForeignKey(u => u.RosterRecordId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
