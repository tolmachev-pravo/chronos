using Chronos.Domain.Models.Abstract;

namespace Chronos.Domain.Models.Issues
{
    public class IssueStatus : IEntity<string>
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public string Key => Id;
    }
}
