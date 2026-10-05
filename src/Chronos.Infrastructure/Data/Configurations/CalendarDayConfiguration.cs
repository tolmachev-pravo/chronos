using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.Infrastructure.Data.Configurations
{
    public class CalendarDayConfiguration : IEntityTypeConfiguration<CalendarDay>
    {
        public void Configure(EntityTypeBuilder<CalendarDay> builder)
        {
            builder
                .HasIndex(day => day.Date)
                .IsUnique();
        }
    }
}
