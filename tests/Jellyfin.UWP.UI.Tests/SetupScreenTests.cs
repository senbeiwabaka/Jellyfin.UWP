using Microsoft.VisualBasic.ApplicationServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.TestUtilities;
using FlaUI.UIA3;

namespace Jellyfin.UWP.UI.Tests
{
    [TestFixture]
    internal sealed class SetupScreenTests : FlaUITestBase
    {
        private const string url = "http://localhost:4567";

        [Test]
        public async Task UrlBoxGetsSet()
        {
            // Arrange
            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            var urlBox = window.FindFirstDescendant("JellyfinUrlTextBox").AsTextBox();

            urlBox.Focus();

            await Task.Delay(500);

            // Act
            Keyboard.Type(url);

            Wait.UntilInputIsProcessed();

            // Assert
            Assert.That(urlBox.Text, Is.EqualTo(url));
        }

        [Test]
        public async Task ButtonIsActive()
        {
            // Arrange
            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            var urlBox = window.FindFirstDescendant("JellyfinUrlTextBox").AsTextBox();

            urlBox.Focus();

            await Task.Delay(200);

            // Act
            Keyboard.Type(url);

            Wait.UntilInputIsProcessed();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(urlBox.Text, Is.EqualTo(url));
                Assert.That(window.FindFirstDescendant("CompleteButton").AsButton().IsEnabled, Is.True);
            });
        }

        [Test]
        public async Task ClickButton_MoveToNextScreen()
        {
            // Arrange
            var window = Application.GetMainWindow(Automation);

            await Setup(window);

            var urlBox = window.FindFirstDescendant("JellyfinUrlTextBox").AsTextBox();
            var button = window.FindFirstDescendant("CompleteButton").AsButton();

            urlBox.Focus();

            await Task.Delay(200);

            Keyboard.Type(url);

            Wait.UntilInputIsProcessed();

            Assert.Multiple(() =>
            {
                Assert.That(urlBox.Text, Is.EqualTo(url));
                Assert.That(button.IsEnabled, Is.True);
            });

            // Act
            button.Click();

            await Task.Delay(200);

            Assert.That(window.FindFirstDescendant("btnChangeURL"), Is.Not.Null);
        }

        protected override AutomationBase GetAutomation()
        {
            return new UIA3Automation();
        }

        protected override Application StartApplication()
        {
            return Application.LaunchStoreApp("37f5d397-a198-4841-bbf2-13fd6f373f27_ab98qgb45jr2w!App");
        }

        public override Task UITestBaseSetUp()
        {
            File.Delete($"c:\\Users\\{Environment.UserName}\\AppData\\Local\\Packages\\37f5d397-a198-4841-bbf2-13fd6f373f27_ab98qgb45jr2w\\Settings\\settings.dat");

            return base.UITestBaseSetUp();
        }

        private static async Task Setup(Window window)
        {
            Assert.That(window, Is.Not.Null);

            await Task.Delay(3000);

            var loginButton = window.FindFirstDescendant("CompleteButton").AsButton();
            var logoutButton = window.FindFirstDescendant("Logout").AsButton();

            // We are on the login screen so we need to go to the url screen to use the specific url
            if (loginButton != null)
            {
                window.FindFirstDescendant("btnChangeURL").AsButton()?.Click();

                return;
            }

            // We are logged in so we need to log out and set the specific url
            if (logoutButton != null)
            {
                logoutButton.Click();

                window.FindFirstDescendant("btnChangeURL").AsButton()?.Click();
            }
        }
    }
}
