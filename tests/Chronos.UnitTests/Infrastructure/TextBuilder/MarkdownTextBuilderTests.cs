using Chronos.Application.TextBuilder;
using Chronos.Infrastructure.TextBuilder;

namespace Chronos.UnitTests.Infrastructure.TextBuilder
{
    [TestFixture]
    public class MarkdownTextBuilderTests
    {
        private MarkdownTextBuilder _textBuilder;

        [SetUp]
        public void SetUp()
        {
            _textBuilder = new MarkdownTextBuilder();
        }

        [TestCase("https://localhost.ru", "site url", $"[site url](https://localhost.ru) ")]
        [TestCase(null, "site url", $"[site url]() ")]
        [TestCase("https://localhost.ru", null, $"[](https://localhost.ru) ")]
        [TestCase(null, null, $"[]() ")]
        public void AddLink_Should_BeCorrect(string? href, string? value, string expected)
        {
            // Arrange
            // Act
            _textBuilder.AddLink(href, value);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("**text** ", "text", TextOption.Bold)]
        [TestCase("****text**** ", "text", TextOption.Bold, TextOption.Bold)]
        [TestCase("text ", "text")]
        [TestCase(" ", "")]
        [TestCase(" ", null)]
        public void AddText_Should_BeCorrect(string expected, string? text, params TextOption[] options)
        {
            // Arrange
            // Act
            _textBuilder.AddText(text, options);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("**", TextOption.Bold)]
        public void BeginOption_Should_BeCorrect(string expected, TextOption option)
        {
            // Arrange
            // Act
            _textBuilder.BeginOption(option);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("**", TextOption.Bold)]
        public void EndOption_Should_BeCorrect(string expected, TextOption option)
        {
            // Arrange
            // Act
            _textBuilder.EndOption(option);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AddNewLine_Should_BeCorrect()
        {
            // Arrange
            // Act
            _textBuilder.AddNewLine();

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(Environment.NewLine));
        }
    }
}
