using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Chronos.Domain.Entities.Calendar;
using Chronos.Infrastructure.Data.Contexts;
using Chronos.Infrastructure.WorkingDays;

namespace Chronos.UnitTests.Infrastructure.WorkingDays
{
    [TestFixture]
    public class WorkingDayRepositoriesTests
    {
        private SqliteConnection _connection;
        private DbContextOptions<ApplicationDbContext> _options;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
        }

        private static CalendarDay Holiday(DateTime date, string title) => new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Kind = CalendarDayKind.Holiday,
            Title = title,
            CreatedAt = DateTime.UtcNow
        };

        private static UserAbsence Absence(string username, DateTime start, DateTime end) => new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            StartDate = start,
            EndDate = end,
            Kind = AbsenceKind.Vacation,
            CreatedAt = DateTime.UtcNow
        };

        [Test]
        public async Task ReplaceYearAsync_ReplacesOnlyThatYear()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new CalendarDayRepository(context);
                await repository.ReplaceYearAsync(2025, new[] { Holiday(new DateTime(2025, 12, 31), "Перенесённый выходной") });
                await repository.ReplaceYearAsync(2026, new[] { Holiday(new DateTime(2026, 6, 12), "Old") });
            }

            using (var context = new ApplicationDbContext(_options))
            {
                await new CalendarDayRepository(context).ReplaceYearAsync(2026, new[]
                {
                    Holiday(new DateTime(2026, 6, 12), "День России"),
                    Holiday(new DateTime(2026, 11, 4), "День народного единства")
                });
            }

            using var assertContext = new ApplicationDbContext(_options);
            var days = await new CalendarDayRepository(assertContext).GetAsync(new DateTime(2025, 1, 1), new DateTime(2026, 12, 31));
            Assert.That(days.Select(day => day.Title), Is.EqualTo(new[]
            {
                "Перенесённый выходной", "День России", "День народного единства"
            }));
        }

        [Test]
        public async Task CalendarGetAsync_IncludesBothEnds()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                await new CalendarDayRepository(context).ReplaceYearAsync(2026, new[]
                {
                    Holiday(new DateTime(2026, 6, 1), "first"),
                    Holiday(new DateTime(2026, 6, 7), "last"),
                    Holiday(new DateTime(2026, 6, 8), "outside")
                });
            }

            using var assertContext = new ApplicationDbContext(_options);
            var days = await new CalendarDayRepository(assertContext)
                .GetAsync(new DateTime(2026, 6, 1), new DateTime(2026, 6, 7, 23, 59, 0));
            Assert.That(days.Select(day => day.Title), Is.EqualTo(new[] { "first", "last" }));
        }

        [Test]
        public async Task AbsenceGetAsync_ReturnsTheUsersAbsencesTouchingThePeriod()
        {
            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new UserAbsenceRepository(context);
                await repository.AddAsync(Absence("john", new DateTime(2026, 5, 25), new DateTime(2026, 6, 1)));
                await repository.AddAsync(Absence("john", new DateTime(2026, 6, 7), new DateTime(2026, 6, 20)));
                await repository.AddAsync(Absence("john", new DateTime(2026, 6, 8), new DateTime(2026, 6, 9)));
                await repository.AddAsync(Absence("john", new DateTime(2026, 5, 1), new DateTime(2026, 5, 10)));
                await repository.AddAsync(Absence("jane", new DateTime(2026, 6, 2), new DateTime(2026, 6, 3)));
            }

            using var assertContext = new ApplicationDbContext(_options);
            var absences = await new UserAbsenceRepository(assertContext)
                .GetAsync("john", new DateTime(2026, 6, 1), new DateTime(2026, 6, 7));
            Assert.That(absences.Select(absence => absence.StartDate), Is.EqualTo(new[]
            {
                new DateTime(2026, 5, 25), new DateTime(2026, 6, 7)
            }));
        }

        [Test]
        public async Task UpdateAsync_ChangesOnlyTheUsersOwnAbsence()
        {
            var absence = Absence("john", new DateTime(2026, 7, 14), new DateTime(2026, 7, 27));
            using (var context = new ApplicationDbContext(_options))
            {
                await new UserAbsenceRepository(context).AddAsync(absence);
            }

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new UserAbsenceRepository(context);
                var foreign = Absence("jane", new DateTime(2026, 8, 1), new DateTime(2026, 8, 2));
                foreign.Id = absence.Id;
                Assert.That(await repository.UpdateAsync(foreign), Is.False);

                var changed = Absence("john", new DateTime(2026, 7, 20), new DateTime(2026, 7, 31));
                changed.Id = absence.Id;
                changed.Kind = AbsenceKind.SickLeave;
                changed.Comment = "Простуда";
                Assert.That(await repository.UpdateAsync(changed), Is.True);
            }

            using var assertContext = new ApplicationDbContext(_options);
            var stored = (await new UserAbsenceRepository(assertContext).ListAsync("john")).Single();
            Assert.Multiple(() =>
            {
                Assert.That(stored.StartDate, Is.EqualTo(new DateTime(2026, 7, 20)));
                Assert.That(stored.EndDate, Is.EqualTo(new DateTime(2026, 7, 31)));
                Assert.That(stored.Kind, Is.EqualTo(AbsenceKind.SickLeave));
                Assert.That(stored.Comment, Is.EqualTo("Простуда"));
            });
        }

        [Test]
        public async Task DeleteAsync_RemovesOnlyTheUsersOwnAbsence()
        {
            var absence = Absence("john", new DateTime(2026, 7, 14), new DateTime(2026, 7, 27));
            using (var context = new ApplicationDbContext(_options))
            {
                await new UserAbsenceRepository(context).AddAsync(absence);
            }

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new UserAbsenceRepository(context);
                Assert.That(await repository.DeleteAsync("jane", absence.Id), Is.False);
                Assert.That(await repository.ListAsync("john"), Has.Count.EqualTo(1));
                Assert.That(await repository.DeleteAsync("john", absence.Id), Is.True);
                Assert.That(await repository.ListAsync("john"), Is.Empty);
            }
        }
    }
}
