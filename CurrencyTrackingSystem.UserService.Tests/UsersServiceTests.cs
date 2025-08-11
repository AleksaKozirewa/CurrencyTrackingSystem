using CurrencyTrackingSystem.Application.DTO.User;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using CurrencyTrackingSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CurrencyTrackingSystem.UserService.Tests
{
    public class UsersServiceTests
    {
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        private readonly UsersService _usersService;

        public UsersServiceTests()
        {
            // Создаем мок для JwtService
            _mockJwtService = new Mock<IJwtService>();

            // Настраиваем InMemory базу данных для тестов
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "UserServiceTestDb")
                .Options;

            // Инициализируем сервис с тестовыми зависимостями
            var dbContext = new AppDbContext(_dbContextOptions);
            _usersService = new UsersService(dbContext, _mockJwtService.Object);
        }

        [Fact]
        public async Task RegisterUserAsync_WithNewUsername_ReturnsSuccessResult()
        {
            // Arrange
            var dto = new UserRegistrationDto
            {
                Username = "newuser",
                Password = "P@ssw0rd123"
            };

            // Act
            var result = await _usersService.RegisterUserAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.NotEqual(Guid.Empty, result.UserId);

            // Проверяем, что пользователь действительно сохранен в БД
            using (var db = new AppDbContext(_dbContextOptions))
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Name == dto.Username);
                Assert.NotNull(user);
                Assert.Equal(dto.Username, user.Name);
            }
        }

        [Fact]
        public async Task RegisterUserAsync_WithExistingUsername_ReturnsError()
        {
            // Arrange
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "existinguser",
                PasswordHash = "hash"
            };

            using (var db = new AppDbContext(_dbContextOptions))
            {
                await db.Users.AddAsync(existingUser);
                await db.SaveChangesAsync();
            }

            var dto = new UserRegistrationDto
            {
                Username = "existinguser",
                Password = "newpassword"
            };

            // Act
            var result = await _usersService.RegisterUserAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Пользователь с таким именем уже существует.", result.ErrorMessage);
            Assert.Equal(Guid.Empty, result.UserId);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsToken()
        {
            var userId = Guid.NewGuid();
            var username = "testuser_" + Guid.NewGuid(); // Уникальное имя
            var password = "correctpassword";

            // Заполняем базу
            await using (var db = new AppDbContext(_dbContextOptions))
            {
                await db.Users.AddAsync(new User
                {
                    Id = userId,
                    Name = username,
                    PasswordHash = password // Сохраняем как есть, если нет хеширования
                });
                await db.SaveChangesAsync();
            }

            var expectedToken = "generated.jwt.token";
            _mockJwtService.Setup(x => x.GenerateToken(userId))
                .Returns(expectedToken);

            var dto = new UserLoginDto
            {
                Username = username, // Используем точно такое же имя
                Password = password  // И точно такой же пароль
            };

            // Act
            var token = await _usersService.LoginAsync(dto);

            // Assert
            Assert.Equal(expectedToken, token);
            _mockJwtService.Verify(
                x => x.GenerateToken(It.Is<Guid>(id => id == userId)),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "testuser",
                PasswordHash = "correctpassword"
            };

            using (var db = new AppDbContext(_dbContextOptions))
            {
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
            }

            var dto = new UserLoginDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            // Act
            var token = await _usersService.LoginAsync(dto);

            // Assert
            Assert.Null(token);
            _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
        {
            // Arrange
            var dto = new UserLoginDto
            {
                Username = "nonexistent",
                Password = "anypassword"
            };

            // Act
            var token = await _usersService.LoginAsync(dto);

            // Assert
            Assert.Null(token);
            _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task IsInvalidatedToken_WithInvalidToken_ReturnsTrue()
        {
            // Arrange
            var invalidToken = "invalid.token";
            var invalidatedTokens = new HashSet<string> { invalidToken };

            _mockJwtService.Setup(x => x.IsInvalidatedToken(invalidToken, invalidatedTokens))
                .Returns(true);

            // Act
            await _usersService.IsInvalidatedToken(invalidToken, invalidatedTokens);

            // Assert
            _mockJwtService.Verify(x => x.IsInvalidatedToken(invalidToken, invalidatedTokens), Times.Once);
        }

        [Fact]
        public async Task Logout_AddsTokenToInvalidatedList()
        {
            // Arrange
            var token = "valid.token";
            var invalidatedTokens = new HashSet<string>();

            _mockJwtService.Setup(x => x.InvalidateToken(token))
                .Callback(() => invalidatedTokens.Add(token));

            // Act
            await _usersService.Logout(token);

            // Assert
            Assert.Contains(token, invalidatedTokens);
            _mockJwtService.Verify(x => x.InvalidateToken(token), Times.Once);
        }

        [Theory]
        [InlineData(null, "password123")] // null username
        [InlineData("", "password123")]   // empty username
        [InlineData("   ", "password123")] // whitespace username
        [InlineData("username", null)]    // null password
        [InlineData("username", "")]      // empty password
        [InlineData("username", "   ")]   // whitespace password
        [InlineData(null, null)]          // both null
        [InlineData("", "")]              // both empty
        public async Task RegisterUserAsync_EmptyOrWhitespaceValues_ReturnsError(string username, string password)
        {
            // Arrange
            var dbName = $"TestDb_{Guid.NewGuid()}"; // Уникальное имя базы для каждого теста
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            await using (var db = new AppDbContext(options))
            {
                var service = new UsersService(db, _mockJwtService.Object);
                var dto = new UserRegistrationDto { Username = username, Password = password };

                // Act
                var result = await service.RegisterUserAsync(dto);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("Имя пользователя и пароль должны быть заполнены.", result.ErrorMessage);

                // Проверяем, что пользователь не был добавлен
                Assert.Empty(db.Users);
            }
        }
    }
}