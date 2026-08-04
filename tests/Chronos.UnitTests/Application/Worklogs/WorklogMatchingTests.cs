using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.UnitTests.Application.Mock;
using Chronos.UnitTests.Application.Extensions;

namespace Chronos.UnitTests.Application.Worklogs
{
	class WorklogMatchingTests
	{
		private IIssue[] _issues;
		private DateTime _date;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var i = 0;
			_date = DateTime.Now.Date;
			_issues = new[]
			{
				new MockIssue($"{i++}"),
				new MockIssue($"{i++}")
			};
		}

		[Test]
		public void Match_WhenParentsAndChildrenAreEmpty_ShouldNotThrow()
		{
			// Arrange
			var parents = Enumerable.Empty<WorkingDayWorklog>();
			var children = Enumerable.Empty<WorkingDayWorklog>();

			// Act
			// Assert
			Assert.That(() => WorklogMatching.Match(parents, children), Throws.Nothing);
		}

		[Test]
		public void Match_WhenParentsAreEmpty_ShouldNotThrow()
		{
			// Arrange
			var parents = Enumerable.Empty<WorkingDayWorklog>();
			var children = new List<WorkingDayWorklog>
			{
				new WorkingDayWorklog { Issue = _issues[0] }
			};

			// Act
			// Assert
			Assert.That(() => WorklogMatching.Match(parents, children), Throws.Nothing);
		}

		[Test]
		public void Match_WhenChildrenAreEmpty_ShouldNotThrow()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				new WorkingDayWorklog { Issue = _issues[0] }
			};
			var children = Enumerable.Empty<WorkingDayWorklog>();

			// Act
			// Assert
			Assert.That(() => WorklogMatching.Match(parents, children), Throws.Nothing);
		}

		[Test]
		public void Match_WhenChildrenAndParentsAreNotEmpty_ShouldMatchByIssueKey()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				new WorkingDayWorklog { Issue = _issues[0] }
			};
			var children = new List<WorkingDayWorklog>
			{
				new WorkingDayWorklog { Issue = _issues[0] }
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.EqualTo(parents[0]));
		}

		[Test]
		public void Match_WhenChildrenAndParentsAreNotEmpty_ShouldMatchByStartDate()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};
			var children = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(15), TimeSpan.FromHours(17))
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.EqualTo(parents[1]));
		}

		[Test]
		public void Match_WhenChildrenAndParentsAreNotEmpty_ShouldMatchByCompleteDate()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};
			var children = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(13), TimeSpan.FromHours(15))
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.EqualTo(parents[1]));
		}

		[Test]
		public void Match_WhenChildrenAndParentsHaveDifferentIssue_ShouldNotMatch()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};
			var children = new List<WorkingDayWorklog>
			{
				_issues[1].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.Null);
		}

		[Test]
		public void Match_WhenChildrenAndParentsAreNotEmpty_ShouldMatchByCompleteDateNesting()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};
			var children = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(13), TimeSpan.FromHours(17))
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.EqualTo(parents[1]));
		}

		[Test]
		public void Match_WhenChildrenAndParentsAreNotEmpty_ShouldMatchByStartDateNesting()
		{
			// Arrange
			var parents = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(14), TimeSpan.FromHours(16))
			};
			var children = new List<WorkingDayWorklog>
			{
				_issues[0].CreateWorkingDayWorklog(_date, TimeSpan.FromHours(8), TimeSpan.FromHours(10)),
			};

			// Act
			WorklogMatching.Match(parents, children);

			// Assert
			Assert.That(children[0].Parent, Is.EqualTo(parents[0]));
		}
	}
}
