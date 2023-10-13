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
        public async Task LoadMediaInformationAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
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

            // Act
            //await sut.LoadMediaInformationAsync(itemId);
            await sut.LoginCommand.ExecuteAsync(CancellationToken.None);

            // Assert
            //Assert.IsNotNull(sut.MediaItem);

            Assert.IsTrue(successRan);
        }
    }
}
