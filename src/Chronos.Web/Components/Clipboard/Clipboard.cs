using Microsoft.JSInterop;
using Chronos.Application.Common.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Thinktecture.Blazor.AsyncClipboard;
using Thinktecture.Blazor.AsyncClipboard.Models;

namespace Chronos.Web.Components.Clipboard
{
    public class Clipboard : IClipboard
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AsyncClipboardService _asyncClipboardService;

        public Clipboard(IJSRuntime jsRuntime,
            AsyncClipboardService asyncClipboardService)
        {
            _jsRuntime = jsRuntime;
            _asyncClipboardService = asyncClipboardService;
        }

        public ValueTask DisposeAsync()
        {
            return _asyncClipboardService.DisposeAsync();
        }

        /// <summary>
        /// Write to clipboard <see cref="ClipboardItemElementCollection"/>
        /// </summary>
        /// <param name="clipboardItemElements"></param>
        /// <returns></returns>
        public async Task WriteAsync(ClipboardItemElementCollection clipboardItemElements)
        {
            try
            {
                var jsClipboard = await _jsRuntime.InvokeAsync<IJSObjectReference>("clipboard");

                var elements = await clipboardItemElements.ToDictionaryAsync(
                    element => element.MimeType.ToEnumString(),
                    element => jsClipboard.InvokeAsync<IJSObjectReference>("convertToPromise", element.Data, element.MimeType));

                var items = new[]
                {
                    new ClipboardItem(elements, new ClipboardItemOptions { PresentationStyle = PresentationStyle.Inline })
                };

                await _asyncClipboardService.WriteAsync(items);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("Clipboard.Write is not supported in this browser", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsSupportedAsync()
        {
            try
            {
                return await _asyncClipboardService.IsSupportedAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
