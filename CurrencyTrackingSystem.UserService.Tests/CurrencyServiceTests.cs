using CurrencyTrackingSystem.Application.DTO.Finance;
using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Domain.Interfaces;
using CurrencyTrackingSystem.Infrastructure.Services;
using Moq;

namespace CurrencyTrackingSystem.UserService.Tests
{
    public class CurrencyServiceTests
    {
        private readonly Mock<ICurrencyRepository> _mockRepo;
        private readonly CurrencyService _service;

        public CurrencyServiceTests()
        {
            _mockRepo = new Mock<ICurrencyRepository>();
            _service = new CurrencyService(_mockRepo.Object);
        }

        [Fact]
        public async Task GetUserFavoriteCurrenciesAsync_ReturnsFavoriteCurrencies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currencies = new List<Currency>
            {
                new Currency { Id = Guid.NewGuid(), Name = "USD", Rate = 75.5m },
                new Currency { Id = Guid.NewGuid(), Name = "EUR", Rate = 85.3m }
            };

            _mockRepo.Setup(x => x.GetUserFavoritesAsync(userId))
                .ReturnsAsync(currencies);

            // Act
            var result = (await _service.GetUserFavoriteCurrenciesAsync(userId)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, x => Assert.True(x.IsFavorite));
            Assert.Equal("USD", result[0].Name);
            Assert.Equal(75.5m, result[0].Rate);
            _mockRepo.Verify(x => x.GetUserFavoritesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task UpdateFavoriteCurrenciesAsync_AddsAndRemovesFavoritesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentFavorites = new List<Currency>
            {
                new Currency { Id = Guid.NewGuid(), Name = "USD" },
                new Currency { Id = Guid.NewGuid(), Name = "EUR" }
            };

            var newFavoriteIds = new List<Guid>
            {
                currentFavorites[0].Id, // Сохраняем USD
                Guid.NewGuid()          // Добавляем новую валюту
            };

            _mockRepo.Setup(x => x.GetUserFavoritesAsync(userId))
                .ReturnsAsync(currentFavorites);

            // Act
            await _service.UpdateFavoriteCurrenciesAsync(
                userId,
                new UpdateFavoriteCurrenciesDto { CurrencyIds = newFavoriteIds });

            // Assert
            _mockRepo.Verify(x => x.AddToFavoritesAsync(
                userId,
                It.Is<Guid>(id => id == newFavoriteIds[1])), Times.Once);

            _mockRepo.Verify(x => x.RemoveFromFavoritesAsync(
                userId,
                It.Is<Guid>(id => id == currentFavorites[1].Id)), Times.Once);

            _mockRepo.Verify(x => x.GetUserFavoritesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task UpdateFavoriteCurrenciesAsync_NoChanges_DoesNothing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentFavorites = new List<Currency>
            {
                new Currency { Id = Guid.NewGuid(), Name = "USD" },
                new Currency { Id = Guid.NewGuid(), Name = "EUR" }
            };

            var newFavoriteIds = currentFavorites.Select(c => c.Id).ToList();

            _mockRepo.Setup(x => x.GetUserFavoritesAsync(userId))
                .ReturnsAsync(currentFavorites);

            // Act
            await _service.UpdateFavoriteCurrenciesAsync(
                userId,
                new UpdateFavoriteCurrenciesDto { CurrencyIds = newFavoriteIds });

            // Assert
            _mockRepo.Verify(x => x.AddToFavoritesAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _mockRepo.Verify(x => x.RemoveFromFavoritesAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }
    }
}
