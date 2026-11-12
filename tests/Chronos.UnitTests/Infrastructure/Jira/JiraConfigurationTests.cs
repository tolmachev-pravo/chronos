using Chronos.Infrastructure.Jira;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Chronos.UnitTests.Infrastructure.Jira
{
    [TestFixture]
    public class JiraConfigurationTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void ResolvedApiUrl_WithoutApiUrl_IsPublicUrl(string? apiUrl)
        {
            var configuration = new JiraConfiguration { Url = "https://jira.test", ApiUrl = apiUrl! };

            Assert.That(configuration.ResolvedApiUrl, Is.EqualTo("https://jira.test"));
        }

        [Test]
        public void ResolvedApiUrl_WithApiUrl_IsApiUrl()
        {
            var configuration = new JiraConfiguration { Url = "https://jira.test", ApiUrl = "http://jira.internal:8080" };

            Assert.That(configuration.ResolvedApiUrl, Is.EqualTo("http://jira.internal:8080"));
        }

        [Test]
        public void ApiUrl_DoesNotChangeBrowserLinks()
        {
            var linkGenerator = new JiraLinkGenerator(Options.Create(
                new JiraConfiguration { Url = "https://jira.test", ApiUrl = "http://jira.internal:8080" }));

            Assert.That(linkGenerator.Generate("ABC-1"), Does.StartWith("https://jira.test"));
        }

        [TestCase("https://jira.test/", "http://jira.internal:8080/",
            "https://jira.test/rest/api/2/user?username=me", "http://jira.internal:8080/rest/api/2/user?username=me")]
        [TestCase("https://jira.test", "http://jira.internal:8080",
            "https://JIRA.test/secure/useravatar?ownerId=me", "http://jira.internal:8080/secure/useravatar?ownerId=me")]
        [TestCase("https://jira.test/", "http://jira.internal:8080/",
            "https://jira.test2/rest/api/2/user", "https://jira.test2/rest/api/2/user")]
        [TestCase("https://jira.test/", "http://jira.internal:8080/",
            "https://www.gravatar.com/avatar/1", "https://www.gravatar.com/avatar/1")]
        [TestCase("https://jira.test/", "",
            "https://jira.test/rest/api/2/user", "https://jira.test/rest/api/2/user")]
        public void ToApiUrl_MovesOnlyPublicJiraLinks(string url, string apiUrl, string link, string expected)
        {
            var configuration = new JiraConfiguration { Url = url, ApiUrl = apiUrl };

            Assert.That(configuration.ToApiUrl(link), Is.EqualTo(expected));
        }

        [Test]
        public void ApiUrl_BindsFromConfiguration()
        {
            var section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jira:Url"] = "https://jira.test",
                    ["Jira:ApiUrl"] = "http://jira.internal:8080"
                })
                .Build()
                .GetSection("Jira");

            var configuration = section.Get<JiraConfiguration>();

            Assert.That(configuration!.ResolvedApiUrl, Is.EqualTo("http://jira.internal:8080"));
        }
    }
}
