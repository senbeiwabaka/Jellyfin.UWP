using FluentAssertions;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.ViewModels.Details;
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
    public sealed class DetailsViewModelTests
    {
        [TestMethod]
        public async Task LoadMediaInformationAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var requestAdapterMock = new Mock<IRequestAdapter>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var apiClient = new JellyfinApiClient(requestAdapterMock.Object);
            var mediaHelpersMock = new Mock<IMediaHelpers>();
            var sut = new DetailsViewModel(memoryCache, apiClient, mediaHelpersMock.Object);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDto>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDto
                {
                    Id = itemId,
                    Tags = new List<string>(),
                    Taglines = new List<string>(),
                    Genres = new List<string>(),
                    People = new List<BaseItemPerson>(),
                    ImageTags = new BaseItemDto_ImageTags { AdditionalData = new Dictionary<string, object> { { "Primary", "" } } },
                    MediaSources = new List<MediaSourceInfo>(),
                })
                .Verifiable();

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDtoQueryResult>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDtoQueryResult
                {
                    Items = new List<BaseItemDto>
                    {
                        new BaseItemDto
                        {
                            Id = Guid.NewGuid(),
                            Tags = new List <string>(),
                            Taglines = new List <string>(),
                            Genres = new List <string>(),
                            People = new List <BaseItemPerson>(),
                            ImageTags = new BaseItemDto_ImageTags{ AdditionalData= new Dictionary<string, object> { { "Primary", "" } } },
                            UserData = new UserItemDataDto{ IsFavorite = false, UnplayedItemCount = 0, Played = false, },
                        },
                    },
                })
                .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);
            Assert.IsFalse(sut.IsMovie);
            Assert.IsFalse(sut.IsEpisode);
            Assert.IsFalse(sut.IsNotMovie);

            requestAdapterMock.VerifyAll();
        }

        [TestMethod]
        public async Task LoadMediaInformationAsync_Movies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var requestAdapterMock = new Mock<IRequestAdapter>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var apiClient = new JellyfinApiClient(requestAdapterMock.Object);
            var mediaHelpersMock = new Mock<IMediaHelpers>();
            var sut = new DetailsViewModel(memoryCache, apiClient, mediaHelpersMock.Object);
            var userData = new UserItemDataDto
            {
                IsFavorite = false,
                PlayCount = 0,
                Played = false,
            };
            var expected = new BaseItemDto
            {
                Id = itemId,
                Tags = new List<string>(),
                Taglines = new List<string>(),
                Genres = new List<string>(),
                People = new List<BaseItemPerson>(),
                ImageTags = new BaseItemDto_ImageTags { AdditionalData = new Dictionary<string, object> { { "Primary", "" } } },
                MediaSources = new List<MediaSourceInfo>(),
                Type = BaseItemDto_Type.Movie,
                MediaSourceCount = 0,
                MediaStreams = new List<MediaStream>(),
            };

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDto>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected)
                .Verifiable();

            // Similar items
            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDtoQueryResult>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDtoQueryResult
                {
                    Items = new List<BaseItemDto>
                    {
                        new BaseItemDto
                        {
                            Id = Guid.NewGuid(),
                            Tags = new List <string>(),
                            Taglines = new List <string>(),
                            Genres = new List <string>(),
                            People = new List <BaseItemPerson>(),
                            ImageTags = new BaseItemDto_ImageTags{ AdditionalData= new Dictionary<string, object> { { "Primary", "" } } },
                            UserData = userData,
                        },
                    },
                })
                .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);

            sut.MediaItem.Should().BeEquivalentTo(expected);

            Assert.IsNull(sut.VideoType);
            Assert.IsNull(sut.RunTime);
            Assert.IsNull(sut.MediaTags);
            Assert.IsNull(sut.MediaTagLines);
            Assert.IsTrue(sut.IsMovie);
            Assert.IsFalse(sut.IsEpisode);
            Assert.IsFalse(sut.IsNotMovie);

            requestAdapterMock.VerifyAll();
        }

        [TestMethod]
        public async Task LoadMediaInformationAsync_Shows()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var requestAdapterMock = new Mock<IRequestAdapter>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var apiClient = new JellyfinApiClient(requestAdapterMock.Object);
            var mediaHelpersMock = new Mock<IMediaHelpers>();
            var sut = new DetailsViewModel(memoryCache, apiClient, mediaHelpersMock.Object);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDto>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDto
                {
                    Id = itemId,
                    Tags = new List<string>(),
                    Taglines = new List<string>(),
                    Genres = new List<string>(),
                    People = new List<BaseItemPerson>(),
                    ImageTags = new BaseItemDto_ImageTags { AdditionalData = new Dictionary<string, object> { { "Primary", "" } } },
                    MediaSources = new List<MediaSourceInfo>(),
                    Type = BaseItemDto_Type.Series,
                })
                .Verifiable();

            requestAdapterMock.Setup(x => x.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<BaseItemDtoQueryResult>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaseItemDtoQueryResult
                {
                    Items = new List<BaseItemDto>
                    {
                        new BaseItemDto
                        {
                            Id = Guid.NewGuid(),
                            Tags = new List <string>(),
                            Taglines = new List <string>(),
                            Genres = new List <string>(),
                            People = new List <BaseItemPerson>(),
                            ImageTags = new BaseItemDto_ImageTags{ AdditionalData= new Dictionary<string, object> { { "Primary", "" } } },
                            UserData = new UserItemDataDto{ IsFavorite = false, UnplayedItemCount = 0, Played = false, },
                        },
                    },
                })
                .Verifiable();

            //tvShowsClientMock.Setup(x => x.GetSeasonsAsync(
            //    itemId,
            //    userId,
            //    new[]
            //        {
            //            ItemFields.ItemCounts,
            //            ItemFields.PrimaryImageAspectRatio,
            //            ItemFields.BasicSyncInfo,
            //            ItemFields.MediaSourceCount,
            //        },
            //    null, null, null, null, null, null, null, default))
            //    .ReturnsAsync(new BaseItemDtoQueryResult { Items = Array.Empty<BaseItemDto>(), })
            //    .Verifiable();

            //tvShowsClientMock.Setup(x => x.GetNextUpAsync(
            //    userId,
            //    null,
            //    null,
            //    new[]
            //        {
            //            ItemFields.MediaSourceCount,
            //        },
            //    itemId,
            //    null, null, null, null, null, null, null, null, null, default))
            //    .ReturnsAsync(new BaseItemDtoQueryResult { Items = Array.Empty<BaseItemDto>(), })
            //    .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);
            Assert.IsFalse(sut.IsMovie);
            Assert.IsFalse(sut.IsEpisode);
            Assert.IsTrue(sut.IsNotMovie);

            requestAdapterMock.VerifyAll();
        }
    }
}
