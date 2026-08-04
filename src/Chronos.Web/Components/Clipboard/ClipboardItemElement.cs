namespace Chronos.Web.Components.Clipboard
{
    /// <summary>
    /// https://developer.mozilla.org/en-US/docs/Web/API/ClipboardItem/ClipboardItem
    /// </summary>
    public class ClipboardItemElement
    {
        /// <summary>
        /// Available mime type of clipboard element
        /// </summary>
        public ClipboardMimeType MimeType { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public string Data { get; set; }
    }
}
