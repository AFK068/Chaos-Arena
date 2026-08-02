using System.Collections.Generic;
using System.IO;
using ChaosArena.Platform;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class MobileOneHandMvpTests
    {
        [Test]
        public void AutoAimHysteresis_OnlySwitchesForMateriallyCloserTarget()
        {
            Assert.That(MobileControlMath.ShouldSwitchAutoAimTarget(100f, 80f), Is.False);
            Assert.That(MobileControlMath.ShouldSwitchAutoAimTarget(100f, 70f), Is.True);
            Assert.That(MobileControlMath.ShouldSwitchAutoAimTarget(-1f, 100f), Is.True);
        }

        [Test]
        public void ResolveDashDirection_PrefersLiveDirectionThenRecentDirection()
        {
            var live = MobileControlMath.ResolveDashDirection(0f, 0.8f, 1f, 0f, 10f, 10.1f);
            var recent = MobileControlMath.ResolveDashDirection(0f, 0f, 1f, 0f, 10f, 10.4f);
            var stale = MobileControlMath.ResolveDashDirection(0f, 0f, 1f, 0f, 10f, 10.46f);

            Assert.That(live.Y, Is.GreaterThan(0f));
            Assert.That(recent.X, Is.EqualTo(1f));
            Assert.That(stale.X, Is.EqualTo(0f));
            Assert.That(stale.Y, Is.EqualTo(0f));
        }

        [Test]
        public void HandPreference_DefaultsRightPersistsToggleAndMirrorsPlacement()
        {
            var store = new FakeHandStore();
            Assert.That(MobileHandPreference.Load(store), Is.EqualTo(MobileHand.Right));
            Assert.That(MobileHandPreference.Toggle(store), Is.EqualTo(MobileHand.Left));
            Assert.That(MobileHandPreference.Load(store), Is.EqualTo(MobileHand.Left));

            var left = MobileControlMath.GetHandPlacement(MobileHand.Left);
            var right = MobileControlMath.GetHandPlacement(MobileHand.Right);
            Assert.That(left.AnchorX, Is.EqualTo(0f));
            Assert.That(right.AnchorX, Is.EqualTo(1f));
            Assert.That(left.HorizontalSign, Is.EqualTo(-right.HorizontalSign));
        }

        [Test]
        public void OneHandMobileLocalization_IsCompleteAndTutorialDescribesAutoAim()
        {
            foreach (var language in new[] { "en", "ru", "tr" })
            {
                Assert.That(LocalizationCatalog.Get(LocalizationCatalog.MobileHandLeft, language), Is.Not.Empty);
                Assert.That(LocalizationCatalog.Get(LocalizationCatalog.MobileHandRight, language), Is.Not.Empty);
                Assert.That(LocalizationCatalog.Get(LocalizationCatalog.TutorialMobile, language), Is.Not.Empty);
            }

            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.TutorialMobile, "en"), Does.Contain("AUTO AIM"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.TutorialMobile, "tr"), Does.Contain("OTOMATİK NİŞAN"));
            Assert.That(FirstRoomTutorialGate.ShouldShow(1, 0, false), Is.True);
            Assert.That(FirstRoomTutorialGate.ShouldShow(1, 0, true), Is.False);
        }

        [Test]
        public void RuntimeContracts_KeepDesktopInputAndUseOneHandAutoAimPath()
        {
            var overlay = ReadProjectFile("Assets/Scripts/UI/MobileControlsController.cs");
            var shoot = ReadProjectFile("Assets/Scripts/Player/PlayerShoot.cs");
            var movement = ReadProjectFile("Assets/Scripts/Player/PlayerMovement.cs");
            var autoAim = ReadProjectFile("Assets/Scripts/UI/MobileAutoAimController.cs");
            var health = ReadProjectFile("Assets/Scripts/PathFinder/EnemyHealth.cs");

            Assert.That(overlay, Does.Not.Contain("AimAndFireStick"));
            Assert.That(overlay, Does.Contain("TryMobileDash"));
            Assert.That(overlay, Does.Contain("SetGameplayActive(visible)"));
            Assert.That(shoot, Does.Contain("manualShootDir"));
            Assert.That(shoot, Does.Contain("mobileAutoAimDir"));
            Assert.That(movement, Does.Contain("ResolveDashDirection"));
            Assert.That(movement, Does.Contain("if (_isDashing || _currentCharges <= 0 || direction.sqrMagnitude < 0.01f) return;"));
            Assert.That(autoAim, Does.Contain("LayerMask.GetMask(\"Walls\")"));
            Assert.That(autoAim, Does.Contain("SetMobileAutoAimDirection(Vector2.zero)"));
            Assert.That(health, Does.Contain("ActiveEnemyRegistry"));
            Assert.That(health, Does.Contain("IsAlive"));
        }

        private static string ReadProjectFile(string path) => File.ReadAllText(Path.GetFullPath(path));

        private sealed class FakeHandStore : IMobileHandPreferenceStore
        {
            private readonly Dictionary<string, string> _values = new();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }
    }
}
