using FluentAssertions;
using Jellyfin.Sdk;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Tests.ViewModels
{
    [TestClass]
    public sealed class LoginViewModelTests
    {
        [TestMethod]
        public async Task LoginCommand()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sdkSettings = new SdkClientSettings
            {
                ClientName = "client",
                DeviceName = "client",
                AccessToken = "token",
                BaseUrl = "http://localhost/",
                ClientVersion = "client",
                DeviceId = "client",
            };
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var userClientMock = new Mock<IUserClient>();
            var sut = new LoginViewModel(
                userClientMock.Object,
                memoryCache,
                sdkSettings);

            memoryCache.Set<UserDto>("user", new UserDto { Id = userId, });

            var successRan = false;

            sut.SuccessfullyLoggedIn += () => { successRan = true; };

            sut.Username = "test";
            sut.Password = "test";

            userClientMock.Setup(x => x.AuthenticateUserByNameAsync(It.IsAny<AuthenticateUserByName>(), It.IsAny<CancellationToken>()))
                .Callback<AuthenticateUserByName, CancellationToken>((x, y) =>
                {
                    x.Should().BeEquivalentTo(new AuthenticateUserByName { Username = sut.Username, Pw = sut.Password, });
                })
                .ReturnsAsync(new AuthenticationResult { AccessToken = "1", SessionInfo = new SessionInfo(), })
                .Verifiable();

            // Act
            await sut.LoginCommand.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(successRan);
            Assert.IsNull(sut.Message);

            userClientMock.VerifyAll();
        }
    }
}
