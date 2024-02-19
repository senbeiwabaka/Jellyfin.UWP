using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.TestUtilities;
using FlaUI.UIA3;
using Jellyfin.Sdk;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Jellyfin.UWP.UI.Tests
{
    [TestFixture]
    internal sealed class MainPageTests : FlaUITestBase
    {
        private const int port = 4567;
        private const string url = "http://localhost:4567";

        private WireMockServer server;

        public override async Task UITestBaseOneTimeSetUp()
        {
            await base.UITestBaseOneTimeSetUp();

            server = WireMockServer.Start(port);
        }

        public override void UITestBaseOneTimeTearDown()
        {
            base.UITestBaseOneTimeTearDown();

            server?.Stop();

            server?.Dispose();
        }

        public override Task UITestBaseSetUp()
        {
            File.Delete($"c:\\Users\\{Environment.UserName}\\AppData\\Local\\Packages\\37f5d397-a198-4841-bbf2-13fd6f373f27_ab98qgb45jr2w\\Settings\\settings.dat");

            return base.UITestBaseSetUp();
        }

        [Test]
        public void MyMediaShouldBeFilled()
        {
            // Arrange
            var window = Application.GetMainWindow(Automation);

            server
                .Given(Request.Create().WithPath(u => u.Contains("/Users/AuthenticateByName")).UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(System.Text.Json.JsonSerializer.Serialize(new AuthenticationResult { AccessToken = "1", })));

            var mediaList = window.FindFirstDescendant("lv_MyMedia");


            // Act

            // Assert
        }

        protected override AutomationBase GetAutomation()
        {
            return new UIA3Automation();
        }

        protected override Application StartApplication()
        {
            return Application.LaunchStoreApp("37f5d397-a198-4841-bbf2-13fd6f373f27_ab98qgb45jr2w!App");
        }

        private static async Task Setup(Window window, WireMockServer server)
        {
            Assert.That(window, Is.Not.Null);

            await Task.Delay(2500);

            var urlBox = window.FindFirstDescendant("JellyfinUrlTextBox").AsTextBox();
            var loginButton = window.FindFirstDescendant("btn_Login").AsButton();

            // We are on the URL Page so move to Login Page
            if (urlBox != null)
            {
                Keyboard.Type(url);

                Wait.UntilInputIsProcessed();

                await Task.Delay(500);

                window.FindFirstDescendant("CompleteButton").AsButton()?.Click();
            }

            await Task.Delay(500);

            // We are on the login screen so move to Main Page
            if (loginButton != null)
            {
                server
                    .Given(Request.Create().WithPath(u => u.Contains("/Users/AuthenticateByName")).UsingPost())
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(System.Text.Json.JsonSerializer.Serialize(new AuthenticationResult { AccessToken = "1", })));

                const string login = "viewer";

                Keyboard.Type(login);

                Wait.UntilInputIsProcessed();

                await Task.Delay(500);

                var passwordBox = window.FindFirstDescendant("p_Password").AsTextBox();

                passwordBox.Focus();

                await Task.Delay(500);

                Keyboard.Type(login);

                Wait.UntilInputIsProcessed();

                await Task.Delay(500);

                window.FindFirstDescendant("btn_Login").AsButton()?.Click();
            }
        }
    }
}
