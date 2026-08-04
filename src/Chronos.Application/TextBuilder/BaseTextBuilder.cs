using System.Linq;
using System.Text;

namespace Chronos.Application.TextBuilder
{
    public abstract class BaseTextBuilder
    {
        protected readonly StringBuilder _stringBuilder;

        public BaseTextBuilder()
        {
            _stringBuilder = new StringBuilder();
        }

        public string Build()
        {
            return _stringBuilder.ToString();
        }
    }
}
