using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Chronos.Application.Users;
using Chronos.Domain.Entities.Users;
using Chronos.Infrastructure.Data.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            ApplicationDbContext dbContext,
            ILogger<UserRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task EnsureUserExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            var exists = await _dbContext.Users
                .AnyAsync(user => user.Username == username, cancellationToken);
            if (exists)
            {
                return;
            }

            _dbContext.Users.Add(new User
            {
                Username = username,
                CreatedAt = DateTime.UtcNow
            });

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogDebug(exception,
                    "Failed to insert user {Username}; likely provisioned concurrently by another request",
                    username);
            }
        }
    }
}
