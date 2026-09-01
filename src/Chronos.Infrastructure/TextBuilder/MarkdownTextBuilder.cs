using Chronos.Application.TextBuilder;
using System;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Chronos.Infrastructure.TextBuilder
{
    public class MarkdownTextBuilder : BaseTextBuilder, ITextBuilder
    {
        public ITextBuilder AddLink(string? href, string? value)
        {
            _stringBuilder.Append($"[{value}]({href})");
            _stringBuilder.Append(' ');
            return this;
        }

        public ITextBuilder AddNewLine()
        {
            _stringBuilder.Append(Environment.NewLine);
            return this;
        }

        public ITextBuilder AddText(string? text, params TextOption[] options)
        {
            foreach (var option in options)
            {
                BeginOption(option);
            }

            _stringBuilder.Append(text);

            foreach (var option in options.Reverse())
            {
                EndOption(option);
            }

            _stringBuilder.Append(' ');

            return this;
        }

        public ITextBuilder BeginOption(TextOption option)
        {
            switch (option)
            {
                case TextOption.Bold:
                    _stringBuilder.Append("**");
                    break;
            }
            return this;
        }

        public ITextBuilder EndOption(TextOption option)
        {
            switch (option)
            {
                case TextOption.Bold:
                    _stringBuilder.Append("**");
                    break;
            }
            return this;
        }
    }
}
