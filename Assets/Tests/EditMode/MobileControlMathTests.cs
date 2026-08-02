using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class MobileControlMathTests
    {
        [Test]
        public void NormalizeStick_ClampsDiagonalBeyondUnitCircle()
        {
            var value = MobileControlMath.NormalizeStick(3f, 4f);

            Assert.That(value.X, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(value.Y, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void NormalizeStick_UsesDeadZone()
        {
            var value = MobileControlMath.NormalizeStick(0.05f, 0.05f);

            Assert.That(value.X, Is.EqualTo(0f));
            Assert.That(value.Y, Is.EqualTo(0f));
        }

        [Test]
        public void ToAnchors_ConvertsNotchedLandscapeSafeArea()
        {
            var area = MobileControlMath.ToAnchors(44f, 0f, 2252f, 1080f, 2400f, 1080f);

            Assert.That(area.MinX, Is.EqualTo(44f / 2400f).Within(0.0001f));
            Assert.That(area.MinY, Is.EqualTo(0f));
            Assert.That(area.MaxX, Is.EqualTo(2296f / 2400f).Within(0.0001f));
            Assert.That(area.MaxY, Is.EqualTo(1f));
        }

        [TestCase(false, false, "", false)]
        [TestCase(true, false, "desktop", true)]
        [TestCase(false, true, "desktop", true)]
        [TestCase(false, false, "mobile", true)]
        [TestCase(false, false, "tablet", true)]
        [TestCase(false, false, "desktop", false)]
        public void IsTouchRuntime_UsesPlatformOrSdkDeviceType(
            bool mobilePlatform, bool handheld, string sdkDeviceType, bool expected)
        {
            Assert.That(MobileControlMath.IsTouchRuntime(mobilePlatform, handheld, sdkDeviceType), Is.EqualTo(expected));
        }

        [Test]
        public void GetBalancedCanvasScale_ProducesUsableCompactLandscapeTargets()
        {
            var scale = MobileControlMath.GetBalancedCanvasScale(844f, 390f);

            Assert.That(scale, Is.EqualTo(0.398f).Within(0.002f));
            Assert.That(144f * scale, Is.GreaterThan(56f));
            Assert.That(260f * scale, Is.GreaterThan(103f));
            Assert.That(112f * scale, Is.GreaterThanOrEqualTo(44f));
        }
    }
}
