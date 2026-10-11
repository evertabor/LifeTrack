using LifeTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeTrack.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    private const int MaxLength = 256;

    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.UserName).HasMaxLength(MaxLength);
        builder.Property(u => u.Email).HasMaxLength(MaxLength);
        builder.Property(u => u.NormalizedEmail).HasMaxLength(MaxLength);
        builder.Property(u => u.NormalizedUserName).HasMaxLength(MaxLength);
        builder.Property(u => u.DisplayName).HasMaxLength(ApplicationUser.DisplayNameMaxLength);
        builder.Property(u => u.PhoneNumber).HasMaxLength(ApplicationUser.PhoneNumberMaxLength);
    }
}
