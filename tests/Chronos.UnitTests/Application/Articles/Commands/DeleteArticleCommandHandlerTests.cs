using Moq;
using Chronos.Application.Articles;
using Chronos.Application.Articles.Commands.DeleteArticle;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.UnitTests.Application.Articles.Commands
{
    [TestFixture]
    public class DeleteArticleCommandHandlerTests
    {
        private DeleteArticleCommandHandler _handler;
        private Mock<IArticleRepository> _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IArticleRepository>();
            _handler = new DeleteArticleCommandHandler(_repository.Object);
        }

        [Test]
        public async Task Handle_Should_ReturnTrue_When_ArticleExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repository
                .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(new DeleteArticleCommand { Id = id }, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _repository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_Should_ReturnFalse_When_ArticleNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repository
                .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(new DeleteArticleCommand { Id = id }, CancellationToken.None);

            // Assert
            Assert.That(result, Is.False);
            _repository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
