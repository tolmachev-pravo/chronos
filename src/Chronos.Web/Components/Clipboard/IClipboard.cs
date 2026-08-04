using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Clipboard
{
    public interface IClipboard
    {
        Task WriteAsync(ClipboardItemElementCollection clipboardItemElements);
        Task<bool> IsSupportedAsync();
    }
}
