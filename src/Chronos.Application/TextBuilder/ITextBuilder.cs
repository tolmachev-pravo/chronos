namespace Chronos.Application.TextBuilder
{
    public interface ITextBuilder
    {
        ITextBuilder AddText(string? text, params TextOption[] options);
        ITextBuilder AddLink(string? href, string? value);
        ITextBuilder BeginOption(TextOption option);
        ITextBuilder EndOption(TextOption option);
        ITextBuilder AddNewLine();
        string Build();
    }
}
