using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.UWP.Helpers;
using Jellyfin.UWP.ViewModels.Details;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Kiota.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
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
            var sut = new DetailsViewModel(memoryCache, apiClient);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });


            //userLibraryClientMock
            //    .Setup(x => x.GetItemAsync(userId, itemId, default))
            //    .ReturnsAsync(new BaseItemDto
            //    {
            //        Id = itemId,
            //        Tags = Array.Empty<string>(),
            //        Taglines = Array.Empty<string>(),
            //        Genres = Array.Empty<string>(),
            //        People = Array.Empty<BaseItemPerson>(),
            //        ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //        MediaSources = Array.Empty<MediaSourceInfo>(),
            //    })
            //    .Verifiable();

            //libraryClientMock.Setup(x => x.GetSimilarItemsAsync(itemId, null, userId, 12, new[] { ItemFields.PrimaryImageAspectRatio }, default))
            //    .ReturnsAsync(new BaseItemDtoQueryResult
            //    {
            //        Items = new[]
            //        {
            //            new BaseItemDto
            //            {
            //                Tags = Array.Empty<string>(),
            //                Taglines = Array.Empty<string>(),
            //                Genres = Array.Empty<string>(),
            //                People = Array.Empty<BaseItemPerson>(),
            //                ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //                UserData = new UserItemDataDto(),
            //            }
            //        }
            //    })
            //    .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);

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
            var sut = new DetailsViewModel(memoryCache, apiClient);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            //userLibraryClientMock
            //    .Setup(x => x.GetItemAsync(userId, itemId, default))
            //    .ReturnsAsync(new BaseItemDto
            //    {
            //        Id = itemId,
            //        Tags = Array.Empty<string>(),
            //        Taglines = Array.Empty<string>(),
            //        Genres = Array.Empty<string>(),
            //        People = Array.Empty<BaseItemPerson>(),
            //        ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //        Type = BaseItemDto_Type.Movie,
            //        MediaStreams = Array.Empty<MediaStream>(),
            //    })
            //    .Verifiable();

            //libraryClientMock.Setup(x => x.GetSimilarItemsAsync(itemId, null, userId, 12, new[] { ItemFields.PrimaryImageAspectRatio }, default))
            //    .ReturnsAsync(new BaseItemDtoQueryResult
            //    {
            //        Items = new[]
            //        {
            //            new BaseItemDto
            //            {
            //                Tags = Array.Empty<string>(),
            //                Taglines = Array.Empty<string>(),
            //                Genres = Array.Empty<string>(),
            //                People = Array.Empty<BaseItemPerson>(),
            //                ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //                UserData = new UserItemDataDto(),
            //            }
            //        }
            //    })
            //    .Verifiable();

            // Act
            await sut.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(sut.MediaItem);
            Assert.IsNull(sut.VideoType);
            Assert.IsNull(sut.RunTime);
            Assert.IsNull(sut.MediaTags);
            Assert.IsNull(sut.MediaTagLines);

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
            var sut = new DetailsViewModel(memoryCache, apiClient);

            memoryCache.Set<UserDto>(JellyfinConstants.UserName, new UserDto { Id = userId, });

            //userLibraryClientMock
            //    .Setup(x => x.GetItemAsync(userId, itemId, default))
            //    .ReturnsAsync(new BaseItemDto
            //    {
            //        Id = itemId,
            //        Tags = Array.Empty<string>(),
            //        Taglines = Array.Empty<string>(),
            //        Genres = Array.Empty<string>(),
            //        People = Array.Empty<BaseItemPerson>(),
            //        ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //        Type = BaseItemDto_Type.Series,
            //        MediaStreams = Array.Empty<MediaStream>(),
            //    })
            //    .Verifiable();

            //libraryClientMock.Setup(x => x.GetSimilarItemsAsync(itemId, null, userId, 12, new[] { ItemFields.PrimaryImageAspectRatio }, default))
            //    .ReturnsAsync(new BaseItemDtoQueryResult
            //    {
            //        Items = new[]
            //        {
            //            new BaseItemDto
            //            {
            //                Tags = Array.Empty<string>(),
            //                Taglines = Array.Empty<string>(),
            //                Genres = Array.Empty<string>(),
            //                People = Array.Empty<BaseItemPerson>(),
            //                ImageTags = new Dictionary<string, string> { { "Primary", "" } },
            //                UserData = new UserItemDataDto(),
            //            }
            //        }
            //    })
            //    .Verifiable();

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

            requestAdapterMock.VerifyAll();
        }
    }
}
