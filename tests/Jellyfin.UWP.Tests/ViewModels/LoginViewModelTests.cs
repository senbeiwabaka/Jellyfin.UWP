using Jellyfin.Sdk;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Tests.ViewModels
{
    [TestClass]
    internal sealed class LoginViewModelTests
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
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var libraryClientMock = new Mock<ILibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new LoginViewModel(
                memoryCache,
                userLibraryClientMock.Object,
                libraryClientMock.Object,
                sdkSettings,
                tvShowsClientMock.Object);

            memoryCache.Set<UserDto>("user", new UserDto { Id = userId, });

            userLibraryClientMock
                .Setup(x => x.GetItemAsync(userId, itemId, default))
                .ReturnsAsync(new BaseItemDto
                {
                    Id = itemId,
                    Tags = Array.Empty<string>(),
                    Taglines = Array.Empty<string>(),
                    Genres = Array.Empty<string>(),
                    People = Array.Empty<BaseItemPerson>(),
                    ImageTags = new Dictionary<string, string> { { "Primary", "" } },
                })
                .Verifiable();

            libraryClientMock.Setup(x => x.GetSimilarItemsAsync(itemId, null, null, 12, new[] { ItemFields.PrimaryImageAspectRatio }, default))
                .ReturnsAsync(new BaseItemDtoQueryResult
                {
                    Items = new[]
                    {
                        new BaseItemDto
                        {
                            Tags = Array.Empty<string>(),
                            Taglines = Array.Empty<string>(),
                            Genres = Array.Empty<string>(),
                            People = Array.Empty<BaseItemPerson>(),
                            ImageTags = new Dictionary<string, string> { { "Primary", "" } },
                        }
                    }
                })
                .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);

            userLibraryClientMock.Verify();
            libraryClientMock.Verify();
        }
    }
}
