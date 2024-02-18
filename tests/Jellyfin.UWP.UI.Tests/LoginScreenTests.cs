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
    internal sealed class LoginScreenTests : FlaUITestBase
    {
        private const int port = 4567;
        private const string url = "http://localhost:4567";

        private WireMockServer server;

        [Test]
        public async Task GoToMainScreen()
        {
            // Arrange
            const string value = "viewer";

            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            server
                .Given(Request.Create().WithPath(u => u.Contains("/Users/AuthenticateByName")).UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(System.Text.Json.JsonSerializer.Serialize(new AuthenticationResult { AccessToken = "1", })));

            // Act
            var userNameTextBox = window.FindFirstDescendant("tb_UserName").AsTextBox();

            userNameTextBox.Focus();

            Keyboard.Type(value);

            Wait.UntilInputIsProcessed();

            await Task.Delay(500);

            var passwordBox = window.FindFirstDescendant("p_Password").AsTextBox();

            passwordBox.Focus();

            await Task.Delay(500);

            Keyboard.Type(value);

            Wait.UntilInputIsProcessed();

            await Task.Delay(500);

            var loginButton = window.FindFirstDescendant("btn_Login").AsButton();

            Assert.That(loginButton.IsEnabled, Is.True);

            loginButton.Click();

            await Task.Delay(1000);

            // Assert
            Assert.That(window.FindFirstDescendant("Logout").AsButton(), Is.Not.Null);
        }

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
        public async Task UsernameGetsSet()
        {
            // Arrange
            const string userName = "viewer";

            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            // Act
            var userNameTextBox = window.FindFirstDescendant("tb_UserName").AsTextBox();

            userNameTextBox.Focus();

            Keyboard.Type(userName);

            Wait.UntilInputIsProcessed();

            // Assert
            Assert.That(window.FindFirstDescendant("tb_UserName").AsTextBox().Text, Is.EqualTo(userName));
        }

        [Test]
        public async Task UsernamePasswordGetsSet()
        {
            // Arrange
            const string value = "viewer";

            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            // Act
            var userNameTextBox = window.FindFirstDescendant("tb_UserName").AsTextBox();

            userNameTextBox.Focus();

            Keyboard.Type(value);

            Wait.UntilInputIsProcessed();

            var passwordBox = window.FindFirstDescendant("p_Password").AsTextBox();

            passwordBox.Focus();

            await Task.Delay(500);

            Keyboard.Type(value);

            Wait.UntilInputIsProcessed();

            await Task.Delay(500);

            // Assert
            Assert.That(window.FindFirstDescendant("btn_Login").AsButton().IsEnabled, Is.True);
        }

        protected override AutomationBase GetAutomation()
        {
            return new UIA3Automation();
        }

        protected override Application StartApplication()
        {
            return Application.LaunchStoreApp("37f5d397-a198-4841-bbf2-13fd6f373f27_ab98qgb45jr2w!App");
        }

        private static async Task Setup(Window window)
        {
            Assert.That(window, Is.Not.Null);

            await Task.Delay(2500);

            var urlBox = window.FindFirstDescendant("JellyfinUrlTextBox").AsTextBox();

            // We are on the url screen so we can specify the specific url
            if (urlBox != null)
            {
                Keyboard.Type(url);

                Wait.UntilInputIsProcessed();

                await Task.Delay(500);

                window.FindFirstDescendant("CompleteButton").AsButton()?.Click();
            }
        }
    }
}
