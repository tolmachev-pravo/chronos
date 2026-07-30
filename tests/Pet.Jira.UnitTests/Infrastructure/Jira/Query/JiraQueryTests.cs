using Pet.Jira.Infrastructure.Jira.Query;

namespace Pet.Jira.UnitTests.Infrastructure.Jira.Query
{
    /// <summary>
    /// The generated JQL is what every Jira search ultimately depends on, so the rendering
    /// of each supported clause is pinned here. Added alongside the ScriptRunner
    /// "commented" function. See issue #259.
    /// </summary>
    [TestFixture]
    public class JiraQueryTests
    {
        private JiraQueryFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new JiraQueryFactory();
        }

        [Test]
        public void ToString_EmptyQuery_ReturnsEmptyString()
        {
            var jql = _factory.Create().ToString();

            Assert.That(jql, Is.Empty);
        }

        [Test]
        public void Where_StringValue_IsQuoted()
        {
            var jql = _factory.Create()
                .Where("type", JiraQueryComparisonType.NotEqual, "Story")
                .ToString();

            Assert.That(jql, Is.EqualTo("type != 'Story' "));
        }

        [Test]
        public void Where_MacrosValue_IsRenderedWithoutQuotes()
        {
            var jql = _factory.Create()
                .Where("assignee", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
                .ToString();

            Assert.That(jql, Is.EqualTo("assignee = currentUser() "));
        }

        [Test]
        public void Where_DateValue_UsesTheDefaultDateFormat()
        {
            var jql = _factory.Create()
                .Where("worklogDate", JiraQueryComparisonType.GreaterOrEqual, new DateTime(2026, 07, 01, 13, 45, 00))
                .ToString();

            Assert.That(jql, Is.EqualTo("worklogDate >= '2026/07/01' "));
        }

        [Test]
        public void Where_MultipleConditions_AreJoinedWithAnd()
        {
            var jql = _factory.Create()
                .Where("assignee", JiraQueryComparisonType.Equal, JiraQueryMacros.CurrentUser)
                .Where("type", JiraQueryComparisonType.NotEqual, "Story")
                .ToString();

            Assert.That(jql, Is.EqualTo("assignee = currentUser() AND type != 'Story' "));
        }

        [Test]
        public void WhereWas_RendersTheDuringClause()
        {
            var jql = _factory.Create()
                .WhereWas("status", "In Progress", new DateTime(2026, 07, 01), new DateTime(2026, 07, 15))
                .ToString();

            Assert.That(jql, Is.EqualTo("status WAS 'In Progress' DURING ('2026/07/01', '2026/07/15') "));
        }

        [Test]
        public void OrderBy_RendersTheOrderClause()
        {
            var jql = _factory.Create()
                .OrderBy("updatedDate", JiraQueryOrderType.Desc)
                .ToString();

            Assert.That(jql, Is.EqualTo("ORDER BY updatedDate DESC "));
        }

        [Test]
        public void WhereCommented_RendersTheScriptRunnerIssueFunction()
        {
            var jql = _factory.Create()
                .WhereCommented(JiraQueryMacros.CurrentUser, new DateTime(2026, 07, 01), new DateTime(2026, 07, 16))
                .ToString();

            Assert.That(jql, Is.EqualTo(
                "issueFunction in commented(\"by currentUser() after 2026/07/01 before 2026/07/16\") "));
        }

        [Test]
        public void WhereCommented_DatesWithTime_AreTruncatedToTheDay()
        {
            var jql = _factory.Create()
                .WhereCommented(JiraQueryMacros.CurrentUser, new DateTime(2026, 07, 01, 23, 59, 59), new DateTime(2026, 07, 16, 00, 00, 01))
                .ToString();

            Assert.That(jql, Is.EqualTo(
                "issueFunction in commented(\"by currentUser() after 2026/07/01 before 2026/07/16\") "));
        }

        [Test]
        public void WhereCommented_CombinedWithConditionsAndOrder_RendersTheFullQuery()
        {
            var jql = _factory.Create()
                .WhereCommented(JiraQueryMacros.CurrentUser, new DateTime(2026, 07, 01), new DateTime(2026, 07, 16))
                .Where("assignee", JiraQueryComparisonType.NotEqual, JiraQueryMacros.CurrentUser)
                .OrderBy("updatedDate", JiraQueryOrderType.Desc)
                .ToString();

            Assert.That(jql, Is.EqualTo(
                "issueFunction in commented(\"by currentUser() after 2026/07/01 before 2026/07/16\") "
                + "AND assignee != currentUser() "
                + "ORDER BY updatedDate DESC "));
        }
    }
}
