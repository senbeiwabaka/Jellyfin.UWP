using Jellyfin.Sdk;
using Jellyfin.UWP.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace Jellyfin.UWP.Tests.ViewModels
{
    [TestClass]
    public sealed class MediaItemPlayerViewModelTests
    {
        [DataTestMethod]
        [DynamicData(nameof(GetAudioData), DynamicDataSourceType.Method)]
        public async Task IsTranscodingNeededBecauseOfAudio_NoSelectedStream(string codec, string codecGuid)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = codec,
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, codecGuid))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord(), mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
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

        [DataTestMethod]
        [DynamicData(nameof(GetAudioData), DynamicDataSourceType.Method)]
        public async Task IsTranscodingNeededBecauseOfAudio_False_SupportedCodec(string codec, string codecGuid)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = codec,
                    Index = 1,
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Audio, CodecCategory.Decoder, codecGuid))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfAudio(new DetailsItemPlayRecord { SelectedAudioMediaStreamIndex = 1 }, mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
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

        [DataTestMethod]
        [DynamicData(nameof(GetVideoData), DynamicDataSourceType.Method)]
        public async Task IsTranscodingNeededBecauseOfVideo_False_NoSelectedStream_SupportedCodec(string codec, string codecGuid)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = codec,
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, codecGuid))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = "123",
                }
            };

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
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
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        [DataTestMethod]
        [DynamicData(nameof(GetVideoData), DynamicDataSourceType.Method)]
        public async Task IsTranscodingNeededBecauseOfVideo_False_SupportedCodec(string codec, string codecGuid)
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var videoClientMock = new Mock<IVideosClient>();
            var playerStateClientMock = new Mock<IPlaystateClient>();
            var mediaInfoClientMock = new Mock<IMediaInfoClient>();
            var subtitleClientMock = new Mock<ISubtitleClient>();
            var userLibraryClientMock = new Mock<IUserLibraryClient>();
            var tvShowsClientMock = new Mock<ITvShowsClient>();
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
            var mediaList = new List<MediaStream>
            {
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = codec,
                    Index = 1,
                }
            };
            var codecQuery = new CodecQuery();
            var audioCodecsInstalled = (await codecQuery.FindAllAsync(CodecKind.Video, CodecCategory.Decoder, codecGuid))
                .Select(x => x).ToArray();
            var expected = !audioCodecsInstalled.Any();

            // Act
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

            // Assert
            Assert.AreEqual(expected, needsTranscoding);
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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
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
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

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
            var sessionClientMock = new Mock<ISessionClient>();
            var sut = new MediaItemPlayerViewModel(
                memoryCache,
                videoClientMock.Object,
                playerStateClientMock.Object,
                mediaInfoClientMock.Object,
                subtitleClientMock.Object,
                userLibraryClientMock.Object,
                tvShowsClientMock.Object,
                sessionClientMock.Object);
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
            var needsTranscoding = await sut.IsTranscodingNeededBecauseOfVideo(mediaList);

            // Assert
            Assert.IsTrue(needsTranscoding);
        }

        public static IEnumerable<object[]> GetAudioData()
        {
            yield return new object[] { "dts", CodecSubtypes.AudioFormatDts };
            yield return new object[] { "aac", CodecSubtypes.AudioFormatAac };
            yield return new object[] { "ac3", CodecSubtypes.AudioFormatDolbyAC3 };
            yield return new object[] { "alac", CodecSubtypes.AudioFormatAlac };
            yield return new object[] { "flac", CodecSubtypes.AudioFormatFlac };
        }

        public static IEnumerable<object[]> GetVideoData()
        {
            yield return new object[] { "mp4v", CodecSubtypes.VideoFormatMP4V };
            yield return new object[] { "h264", CodecSubtypes.VideoFormatH264 };
            yield return new object[] { "hevc", CodecSubtypes.VideoFormatHevc };
            yield return new object[] { "h263", CodecSubtypes.VideoFormatH263 };
        }
    }
}
