using Moq;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Dto;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class JiraIssueDataSourceTests
    {
        private JiraIssueDataSource _dataSource;
        private IJiraService _jiraService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var applicationType = "gitlabselfmanaged";
            var jiraServiceMock = new Mock<IJiraService>();
            jiraServiceMock
                .Setup(mock => mock.GetIssueDevStatusDetailAsync(It.IsAny<string>(), applicationType, "pullrequest", default))
                .Returns((string jiraIdentifier, string applicationType,
                    string dataType, CancellationToken cancellationToken) =>
                {
                    switch (jiraIdentifier)
                    {
                        // Empty dto
                        case "1":
                            return Task.FromResult(new DevStatusDetailDto());
                        // Empty detail
                        case "2":
                            return Task.FromResult(new DevStatusDetailDto { Detail = null });
                        // No github detail
                        case "3":
                            return Task.FromResult(new DevStatusDetailDto
                            {
                                Detail = new[] {
                                    new Detail
                                    {
                                        Instance = new Instance
                                        {
                                            Id = "mail"
                                        }
                                    }}
                            });
                        // No open pull request
                        case "4":
                            return Task.FromResult(new DevStatusDetailDto
                            {
                                Detail = new[] {
                                    new Detail
                                    {
                                        Instance = new Instance
                                        {
                                            Id = applicationType
                                        },
                                        PullRequests = new []
                                        {
                                            new PullRequest
                                            {
                                                Status = "INREVIEW",
                                                Url = new Uri("https://unit-test.com/4")
                                            }
                                        }
                                    }}
                            });
                        // More than 1 open pull requests
                        case "5":
                            return Task.FromResult(new DevStatusDetailDto
                            {
                                Detail = new[] {
                                    new Detail
                                    {
                                        Instance = new Instance
                                        {
                                            Id = applicationType
                                        },
                                        PullRequests = new []
                                        {
                                            new PullRequest
                                            {
                                                Status = "OPEN",
                                                Url = new Uri("https://unit-test.com/5-1")
                                            },
                                            new PullRequest
                                            {
                                                Status = "OPEN",
                                                Url = new Uri("https://unit-test.com/5-2")
                                            }
                                        }
                                    }}
                            });
                        // Only one open pull request
                        case "6":
                            return Task.FromResult(new DevStatusDetailDto
                            {
                                Detail = new[] {
                                    new Detail
                                    {
                                        Instance = new Instance
                                        {
                                            Id = applicationType
                                        },
                                        PullRequests = new []
                                        {
                                            new PullRequest
                                            {
                                                Status = "OPEN",
                                                Url = new Uri("https://unit-test.com/6")
                                            }
                                        }
                                    }}
                            });
                        // Only one open pull request
                        case "7":
                            return Task.FromResult(new DevStatusDetailDto
                            {
                                Detail = new[] {
                                    new Detail
                                    {
                                        Instance = new Instance
                                        {
                                            Id = applicationType
                                        },
                                        PullRequests = new []
                                        {
                                            new PullRequest
                                            {
                                                Status = "CLOSED",
                                                Url = new Uri("https://unit-test.com/7-1")
                                            },
                                            new PullRequest
                                            {
                                                Status = "OPEN",
                                                Url = new Uri("https://unit-test.com/7-2")
                                            },
                                            new PullRequest
                                            {
                                                Status = "DECLINED",
                                                Url = new Uri("https://unit-test.com/7-3")
                                            },
                                        }
                                    }}
                            });
                    }
                    return Task.FromResult(new DevStatusDetailDto());
                });
            _jiraService = jiraServiceMock.Object;
            _dataSource = new JiraIssueDataSource(_jiraService);
        }

        [TestCase("1", null)]
        [TestCase("2", null)]
        [TestCase("3", null)]
        [TestCase("4", null)]
        [TestCase("5", null)]
        [TestCase("6", "https://unit-test.com/6")]
        [TestCase("7", "https://unit-test.com/7-2")]
        public async Task GetIssueOpenPullRequestUrlAsync_Should_BeCorrect(string identifier, string? expected)
        {
            // Arrange
            var query = new Chronos.Application.Issues.Queries.GetIssueOpenPullRequestUrl.Query
            {
                Identifier = identifier
            };

            // Act
            var url = await _dataSource.GetIssueOpenPullRequestUrlAsync(query);

            // Assert
            Assert.That(url, Is.EqualTo(expected));
        }
    }
}
