using FluentAssertions;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.Models;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.UWP.Tests.ViewModels
{
    [TestClass]
    public sealed class MediaListViewModelTests
    {
        [TestMethod]
        public async Task GetLatestOnItemAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            var requestAdapterMock = new Mock<IRequestAdapter>();
            var apiClient = new JellyfinApiClient(requestAdapterMock.Object);
            var mediaHelpersMock = new Mock<IMediaHelpers>();
            var sut = new MediaListViewModel(memoryCache, apiClient, mediaHelpersMock.Object);
            var expected = new UIMediaListItem
            {
                Id = itemId,
                Name = "test",
                Type = BaseItemDto_Type.Episode,
                UserData = new UIUserData
                {
                    HasBeenWatched = true,
                },
            };

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDto>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDto
                {
                    Id = itemId,
                    Name = "test",
                    Type = BaseItemDto_Type.Episode,
                    Tags = new List<string>(),
                    Taglines = new List<string>(),
                    Genres = new List<string>(),
                    People = new List<BaseItemPerson>(),
                    ImageTags = new BaseItemDto_ImageTags { AdditionalData = new Dictionary<string, object> { { "Primary", "" } } },
                    MediaSources = new List<MediaSourceInfo>(),
                    UserData = new UserItemDataDto
                    {
                        IsFavorite = false,
                        Played = true,
                    },
                })
                .Verifiable();

            // Act
            var result = await sut.GetLatestOnItemAsync(itemId);

            // Assert
            result.Should().BeEquivalentTo(expected);

            requestAdapterMock.VerifyAll();
        }
    }
}
