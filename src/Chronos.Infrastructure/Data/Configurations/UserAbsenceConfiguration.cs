using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.Infrastructure.Data.Configurations
{
    public class UserAbsenceConfiguration : IEntityTypeConfiguration<UserAbsence>
    {
        public void Configure(EntityTypeBuilder<UserAbsence> builder)
        {
            builder
                .HasIndex(absence => new { absence.Username, absence.StartDate });
        }
    }
}
