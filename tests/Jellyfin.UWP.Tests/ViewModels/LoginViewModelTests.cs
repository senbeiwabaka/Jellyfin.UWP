using FluentAssertions;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions;
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
            var requestAdapterMock = new Mock<IRequestAdapter>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var apiClient = new JellyfinApiClient(requestAdapterMock.Object);
            var jellyfinSettings = new JellyfinSdkSettings();
            var sut = new LoginViewModel(memoryCache, apiClient, jellyfinSettings);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            var successRan = false;

            sut.SuccessfullyLoggedIn += () => { successRan = true; };

            sut.Username = "test";
            sut.Password = "test";

            //userClientMock.Setup(x => x.AuthenticateUserByNameAsync(It.IsAny<AuthenticateUserByName>(), It.IsAny<CancellationToken>()))
            //    .Callback<AuthenticateUserByName, CancellationToken>((x, y) =>
            //    {
            //        x.Should().BeEquivalentTo(new AuthenticateUserByName { Username = sut.Username, Pw = sut.Password, });
            //    })
            //    .ReturnsAsync(new AuthenticationResult { AccessToken = "1", SessionInfo = new SessionInfo(), })
            //    .Verifiable();

            // Act
            await sut.LoginCommand.ExecuteAsync(CancellationToken.None);

            // Assert
            Assert.IsTrue(successRan);
            Assert.IsNull(sut.Message);

            requestAdapterMock.VerifyAll();
        }
    }
}
