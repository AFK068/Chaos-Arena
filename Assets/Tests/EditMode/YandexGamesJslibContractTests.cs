using System.IO;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class YandexGamesJslibContractTests
    {
        [Test]
        public void FullscreenBridge_UsesTheOfficialCallbacksShapeAndEscapedTerminalPayload()
        {
            var source = File.ReadAllText("Assets/Plugins/WebGL/YandexGames.jslib");

            Assert.That(source, Does.Contain("showFullscreenAdv({"));
            Assert.That(source, Does.Contain("callbacks: {"));
            Assert.That(source, Does.Contain("onOpen:"));
            Assert.That(source, Does.Contain("onClose: function (wasShown)"));
            Assert.That(source, Does.Contain("onError:"));
            Assert.That(source, Does.Contain("JSON.stringify({ requestId: requestId, result: result })"));
            Assert.That(source, Does.Contain("YandexGames_SetFullscreenAdReceiver"));
        }

        [Test]
        public void DeviceBridge_UsesYandexDeviceInfoInsteadOfTouchscreenPresence()
        {
            var jslib = File.ReadAllText("Assets/Plugins/WebGL/YandexGames.jslib");
            var controls = File.ReadAllText("Assets/Scripts/UI/MobileControlsController.cs");

            Assert.That(jslib, Does.Contain("sdk.deviceInfo.type"));
            Assert.That(jslib, Does.Contain("OnYandexGamesDeviceTypeDetected"));
            Assert.That(controls, Does.Contain("YandexPlatformService.TouchDeviceReady"));
            Assert.That(controls, Does.Not.Contain("Touchscreen.current"));
        }

        [Test]
        public void MainMenu_HasNoBrowserQuitAction()
        {
            var menu = File.ReadAllText("Assets/Scripts/UI/MainMenuUI.cs");
            var scene = File.ReadAllText("Assets/Scenes/MainMenu.unity");

            Assert.That(menu, Does.Not.Contain("Application.Quit"));
            Assert.That(menu, Does.Not.Contain("OnQuit"));
            Assert.That(scene, Does.Not.Contain("Button_Quit"));
        }

        [Test]
        public void PlayerBridge_AcquiresGuestWithoutScopesThenReadsAndWritesAggregateProgress()
        {
            var source = File.ReadAllText("Assets/Plugins/WebGL/YandexGames.jslib");

            Assert.That(source, Does.Contain("YandexGames_SetPlayerReceiver"));
            Assert.That(source, Does.Contain("YandexGames_PlayerGetGuest"));
            Assert.That(source, Does.Contain("YandexGames_PlayerGetData"));
            Assert.That(source, Does.Contain("YandexGames_PlayerSetData"));
            Assert.That(source, Does.Contain("bridge.sdk.getPlayer({ scopes: false })"));
            Assert.That(source, Does.Contain("player.getData([progressKey])"));
            Assert.That(source, Does.Contain("player.setData(data, true)"));
            Assert.That(source, Does.Contain("operation: 'getPlayer'"));
            Assert.That(source, Does.Contain("operation: 'setData'"));
            Assert.That(source, Does.Contain("JSON.stringify({"));
        }

        [Test]
        public void PlayerCloudFlow_HasNoSignInSurfaceAndCoordinatorAlwaysFlushesAfterBaseline()
        {
            var jslib = File.ReadAllText("Assets/Plugins/WebGL/YandexGames.jslib");
            var coordinator = File.ReadAllText("Assets/Scripts/Platform/Progress/CloudProgressSyncCoordinator.cs");
            var service = File.ReadAllText("Assets/Scripts/Platform/YandexPlayerService.cs");
            var menu = File.ReadAllText("Assets/Scripts/UI/MainMenuUI.cs");
            var catalog = File.ReadAllText("Assets/Scripts/Platform/LocalizationCatalog.cs");

            Assert.That(jslib, Does.Not.Contain("PlayerAuthorize"));
            Assert.That(jslib, Does.Not.Contain("openAuthDialog"));
            Assert.That(jslib, Does.Not.Contain("player.authorize"));
            Assert.That(coordinator, Does.Not.Contain("RequestAuthorization"));
            Assert.That(coordinator, Does.Not.Contain("CanRequestSignIn"));
            Assert.That(service, Does.Not.Contain("RequestCloudSignIn"));
            Assert.That(menu, Does.Not.Contain("CloudSaveSettingsUI"));
            Assert.That(catalog, Does.Not.Contain("settings.cloud_save"));
            Assert.That(coordinator, Does.Contain("_dirtyDocument = _snapshotSerialized();"));
            Assert.That(coordinator, Does.Contain("TryWriteDirty();"));
        }

        [Test]
        public void PlayerReceiver_SourceConsumesEachKnownRequestIdOnlyOnce()
        {
            var source = File.ReadAllText("Assets/Scripts/Platform/YandexPlayerService.cs");

            Assert.That(source, Does.Contain("RequestTimeoutSeconds = 15f"));
            Assert.That(source, Does.Contain("ExpirePendingRequests"));
            Assert.That(source, Does.Contain("_pending.TryGetValue(response.requestId, out var pending)"));
            Assert.That(source, Does.Contain("_pending.Remove(response.requestId)"));
            Assert.That(source, Does.Contain("Unknown/late/duplicate ids are ignored"));
        }
    }
}
