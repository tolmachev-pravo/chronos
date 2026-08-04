using Chronos.Application.TextBuilder;
using Chronos.Infrastructure.TextBuilder;

namespace Chronos.UnitTests.Infrastructure.TextBuilder
{
    [TestFixture]
    public class HtmlTextBuilderTests
    {
        private HtmlTextBuilder _textBuilder;

        [SetUp]
        public void SetUp()
        {
            _textBuilder = new HtmlTextBuilder();
        }

        [TestCase("https://localhost.ru", "site url", $"<a href=\"https://localhost.ru\">site url</a> ")]
        [TestCase(null, "site url", $"<a href=\"\">site url</a> ")]
        [TestCase("https://localhost.ru", null, $"<a href=\"https://localhost.ru\"></a> ")]
        [TestCase(null, null, $"<a href=\"\"></a> ")]
        public void AddLink_Should_BeCorrect(string? href, string? value, string expected)
        {
            // Arrange
            // Act
            _textBuilder.AddLink(href, value);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("<b>text</b> ", "text", TextOption.Bold)]
        [TestCase("<b><b>text</b></b> ", "text", TextOption.Bold, TextOption.Bold)]
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

        [TestCase("<b>", TextOption.Bold)]
        public void BeginOption_Should_BeCorrect(string expected, TextOption option)
        {
            // Arrange
            // Act
            _textBuilder.BeginOption(option);

            // Assert
            var result = _textBuilder.Build();
            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase("</b>", TextOption.Bold)]
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
            Assert.That(result, Is.EqualTo("<br/>"));
        }
    }
}
