using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Chronos.Domain.Entities.Blog;
using Chronos.Domain.Entities.Notifications;
using Chronos.Domain.Entities.Extensions;
using Chronos.Domain.Entities.Users;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Data.Contexts
{
	public class ApplicationDbContext : DbContext
	{
		private IDbContextTransaction _currentTransaction;

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			  : base(options)
		{
		}

		public DbSet<User> Users { get; set; }
		public DbSet<UserNotification> UserNotifications { get; set; }
		public DbSet<Article> Articles { get; set; }
		public DbSet<UserExtension> UserExtensions { get; set; }

		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return base.SaveChangesAsync(cancellationToken);
		}

		public async Task BeginTransactionAsync()
		{
			if (_currentTransaction != null)
			{
				return;
			}

			_currentTransaction = await base.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);
		}

		public async Task CommitTransactionAsync()
		{
			try
			{
				await SaveChangesAsync().ConfigureAwait(false);

				_currentTransaction?.Commit();
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
			finally
			{
				if (_currentTransaction != null)
				{
					_currentTransaction.Dispose();
					_currentTransaction = null;
				}
			}
		}

		public void RollbackTransaction()
		{
			try
			{
				_currentTransaction?.Rollback();
			}
			finally
			{
				if (_currentTransaction != null)
				{
					_currentTransaction.Dispose();
					_currentTransaction = null;
				}
			}
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

			base.OnModelCreating(builder);
		}
	}
}
