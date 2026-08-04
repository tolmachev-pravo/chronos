using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chronos.Infrastructure.Data.Contexts
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // Keep in step with ServiceCollectionExtensions: the pre-Chronos file name is
            // deliberate, see the comment there before changing it.
            optionsBuilder.UseSqlite("Data Source = JiraCopilot.sqlite3");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
