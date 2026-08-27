using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chronos.Domain.Entities.Users;

namespace Chronos.Infrastructure.Data.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder
                .HasIndex(settings => settings.Username)
                .IsUnique();
        }
    }
}
