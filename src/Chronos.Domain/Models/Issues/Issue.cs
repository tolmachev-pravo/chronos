using Chronos.Domain.Models.Abstract;

namespace Chronos.Domain.Models.Issues
{
    public class Issue : IIssue, IEntity<string>
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Link { get; set; }
        public string Identifier { get; set; }

		public string Name => $"{Key}. {Summary}";
	}
}
