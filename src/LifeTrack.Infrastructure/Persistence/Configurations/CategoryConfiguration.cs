using LifeTrack.Domain.Finance;
using LifeTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeTrack.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(10).IsRequired().HasDefaultValue(TransactionType.Expense);
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
