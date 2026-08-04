using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chronos.Domain.Entities.Extensions;

namespace Chronos.Infrastructure.Data.Configurations
{
    public class UserExtensionConfiguration : IEntityTypeConfiguration<UserExtension>
    {
        public void Configure(EntityTypeBuilder<UserExtension> builder)
        {
            builder
                .HasIndex(e => new { e.Username, e.Type })
                .IsUnique();
        }
    }
}
