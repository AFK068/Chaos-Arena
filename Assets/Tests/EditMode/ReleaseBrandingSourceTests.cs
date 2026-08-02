using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ChaosArena.Platform.Tests
{
    public sealed class ReleaseBrandingSourceTests
    {
        private const string IconPath = "Assets/Branding/Yandex/chaos-arena-icon-512.png";
        private const string CoverPath = "Assets/Branding/Yandex/chaos-arena-cover-800x470.png";
        private const string TemplatePath = "Assets/WebGLTemplates/Yandex/index.html";

        [Test]
        public void PlayerSettings_UseReleaseIdentityAndYandexTemplate()
        {
            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

            Assert.That(settings, Does.Contain("companyName: Antion"));
            Assert.That(settings, Does.Contain("bundleVersion: 0.0.0.1"));
            Assert.That(settings, Does.Contain("webGLTemplate: PROJECT:Yandex"));
        }

        [TestCase(IconPath, 512, 512)]
        [TestCase(CoverPath, 800, 470)]
        public void YandexReleaseImages_HaveRequiredDimensions(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture!.width, Is.EqualTo(width), path);
            Assert.That(texture.height, Is.EqualTo(height), path);
        }

        [Test]
        public void WebGlTemplate_IsFullscreenTouchSafeAndUsesProjectBranding()
        {
            var source = File.ReadAllText(TemplatePath);

            Assert.That(source, Does.Contain("viewport-fit=cover"));
            Assert.That(source, Does.Contain("touch-action: none"));
            Assert.That(source, Does.Contain("TemplateData/favicon.png"));
            Assert.That(source, Does.Contain("createUnityInstance"));
            Assert.That(source, Does.Contain("autoSyncPersistentDataPath: true"));
            Assert.That(source, Does.Contain("contextmenu"));
            Assert.That(source, Does.Not.Contain("unity-logo"));
        }
    }
}
