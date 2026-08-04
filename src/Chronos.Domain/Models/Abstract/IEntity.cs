namespace Chronos.Domain.Models.Abstract
{
    public interface IEntity<TKey>
    {
        TKey Key { get; }
    }
}
