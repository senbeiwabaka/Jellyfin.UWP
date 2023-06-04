using Jellyfin.Sdk;
using Jellyfin.UWP.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
            var mockFactory = new Mock<IHttpClientFactory>();
            var sdkSettings = new SdkClientSettings
            {
                ClientName = "client",
                DeviceName = "client",
                AccessToken = "token",
                BaseUrl = "http://localhost/api/user/",
                ClientVersion = "client",
                DeviceId = "client",
            };
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var viewModel = new DetailsViewModel(mockFactory.Object, sdkSettings, memoryCache);
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("http://localhost/api/user/*")
                .Respond("application/json", JsonConvert.SerializeObject(new BaseItemDto
                {
                    Tags = Array.Empty<string>(),
                    Taglines = Array.Empty<string>(),
                    Genres = Array.Empty<string>(),
                    People = Array.Empty<BaseItemPerson>(),
                    ImageTags = new Dictionary<string, string> { { "Primary", "" } },
                }));

            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());

            memoryCache.Set<UserDto>("user", new UserDto());

            // Act
            await viewModel.LoadMediaInformationAsync(Guid.NewGuid());

            // Assert
            Assert.IsNotNull(viewModel.MediaItem);
        }
    }
}
