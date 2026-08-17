using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Askii.Database.Configuration;

public class OptionsConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.ToTable("Options");
        builder.HasKey(x => x.Name);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Value)
            .IsRequired()
            .HasDefaultValue(string.Empty)
            .HasMaxLength(100);

        builder.Property(p => p.LastUpdateUtc)
            .IsRequired(true)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}