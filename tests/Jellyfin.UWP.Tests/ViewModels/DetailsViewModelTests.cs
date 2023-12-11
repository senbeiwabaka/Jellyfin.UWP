﻿using Jellyfin.Sdk;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
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
            var viewModel = new DetailsViewModel(
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
            await viewModel.LoadMediaInformationAsync(itemId);

            // Assert
            Assert.IsNotNull(viewModel.MediaItem);

            userLibraryClientMock.Verify();
            libraryClientMock.Verify();
        }

        [TestMethod]
        public async Task LoadMediaInformationAsync_Movies()
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
            var sut = new DetailsViewModel(
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
                    Type = BaseItemKind.Movie,
                    MediaStreams = Array.Empty<MediaStream>(),
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
            Assert.IsNull(sut.VideoType);
            Assert.IsNull(sut.RunTime);
            Assert.IsNull(sut.MediaTags);
            Assert.IsNull(sut.MediaTagLines);

            userLibraryClientMock.Verify();
            libraryClientMock.Verify();
        }

        [TestMethod]
        public async Task LoadMediaInformationAsync_Shows()
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
            var sut = new DetailsViewModel(
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
                    Type = BaseItemKind.Series,
                    MediaStreams = Array.Empty<MediaStream>(),
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

            tvShowsClientMock.Setup(x => x.GetSeasonsAsync(
                itemId,
                userId,
                new[]
                    {
                        ItemFields.ItemCounts,
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.BasicSyncInfo,
                        ItemFields.MediaSourceCount,
                    },
                null, null, null, null, null, null, null, default))
                .ReturnsAsync(new BaseItemDtoQueryResult { Items = Array.Empty<BaseItemDto>(), })
                .Verifiable();

            tvShowsClientMock.Setup(x => x.GetNextUpAsync(
                userId,
                null,
                null,
                new[]
                    {
                        ItemFields.MediaSourceCount,
                    },
                itemId,
                null, null, null, null, null, null, null, null, null, default))
                .ReturnsAsync(new BaseItemDtoQueryResult { Items = Array.Empty<BaseItemDto>(), })
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
