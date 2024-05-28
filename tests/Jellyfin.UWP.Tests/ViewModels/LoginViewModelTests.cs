using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.ViewModels;
using Moq;
using Windows.Storage;

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

            RequestInformation postRequest = null;

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            var successRan = false;

            sut.SuccessfullyLoggedIn += () => { successRan = true; };

            sut.Username = "test";
            sut.Password = "test";

            requestAdapterMock.Setup(x => x.SerializationWriterFactory.GetSerializationWriter("application/json"))
                .Returns(new JsonSerializationWriter());

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<AuthenticationResult>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .Callback(new InvocationAction(invocation =>
                {
                    postRequest = (RequestInformation)invocation.Arguments[0];
                }))
                .ReturnsAsync(new AuthenticationResult { AccessToken = "1", SessionInfo = new SessionInfo(), })
                .Verifiable();

            ApplicationData.Current.LocalSettings.Values[JellyfinConstants.HostUrlName] = "https://test.com";

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
            Assert.IsNotNull(postRequest);

            requestAdapterMock.VerifyAll();
        }
    }
}
