namespace Chronos.Domain.Entities.Extensions
{
    public class UserExtension : BaseEntity
    {
        public string Username { get; set; }
        public ExtensionType Type { get; set; }
        public bool IsEnabled { get; set; }
        public string Settings { get; set; }  // JSON, structure depends on Type
    }
}
