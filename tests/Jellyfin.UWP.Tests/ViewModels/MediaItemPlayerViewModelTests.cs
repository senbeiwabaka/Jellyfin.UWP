using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Moq;
using Windows.Media.Core;

namespace Jellyfin.UWP.Tests.ViewModels
{
    [TestClass]
    public sealed class MediaItemPlayerViewModelTests
    {
        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_False_NoSelectedStream()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "aac",
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_False_NoSelectedStream_UnsupportedDTS()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "dts",
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, CodecSubtypes.AudioFormatDts))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
        }

        [TestMethod]
        [DataRow("aac")]
        [DataRow("ac3")]
        [DataRow("alac")]
        [DataRow("flac")]
        public async Task IsTranscodingNeededBecauseOfAudio_False_NoSelectedStream_SupportedCodec(string codec)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = codec,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_True_NoSelectedStream()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "123",
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_False()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "aac",
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_False_UnsupportedDTS()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "dts",
                    Index = 1,
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, CodecSubtypes.AudioFormatDts))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
        }

        [TestMethod]
        [DataRow("aac")]
        [DataRow("ac3")]
        [DataRow("alac")]
        [DataRow("flac")]
        public async Task IsTranscodingNeededBecauseOfAudio_False_SupportedCodec(string codec)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = codec,
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfAudio_True()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = "123",
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_False_NoSelectedStream()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        [DataRow("mp4v")]
        [DataRow("h264")]
        [DataRow("hevc")]
        [DataRow("h263")]
        public async Task IsTranscodingNeededBecauseOfVideo_False_NoSelectedStream_SupportedCodec(string codec)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = codec,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_True_NoSelectedStream()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "123",
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_True_NoSelectedStream_Unsupported10Bit()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                    BitDepth = 10,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_False()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        [DataRow("mp4v")]
        [DataRow("h264")]
        [DataRow("hevc")]
        [DataRow("h263")]
        public async Task IsTranscodingNeededBecauseOfVideo_False_SupportedCodec(string codec)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = codec,
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsFalse(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_True()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "123",
                    Index = 1,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [TestMethod]
        public async Task IsTranscodingNeededBecauseOfVideo_True_Unsupported10Bit()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "hevc",
                    Index = 1,
                    BitDepth = 10,
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }
    }
}
