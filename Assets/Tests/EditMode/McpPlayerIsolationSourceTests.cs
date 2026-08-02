using System.IO;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class McpPlayerIsolationSourceTests
    {
        private const string EmbeddedPackagePath = "Packages/com.ivanmurzak.unity.mcp";

        [Test]
        public void EmbeddedMcpPackage_IsPinnedAndRuntimeIsEditorOnly()
        {
            var package = File.ReadAllText(Path.Combine(EmbeddedPackagePath, "package.json"));
            var asmdef = File.ReadAllText(Path.Combine(EmbeddedPackagePath, "Runtime/com.IvanMurzak.Unity.MCP.Runtime.asmdef"));
            var testFilesAsmdef = File.ReadAllText(Path.Combine(EmbeddedPackagePath, "TestFiles/com.IvanMurzak.Unity.MCP.TestFiles.asmdef"));
            var manifest = File.ReadAllText("Packages/manifest.json");
            var packageLock = File.ReadAllText("Packages/packages-lock.json");

            Assert.That(package, Does.Contain("\"version\": \"0.86.3\""));
            Assert.That(manifest, Does.Contain("\"com.ivanmurzak.unity.mcp\": \"0.86.3\""));
            Assert.That(asmdef, Does.Contain("\"includePlatforms\": [\n        \"Editor\"\n    ]"));
            Assert.That(testFilesAsmdef, Does.Contain("\"includePlatforms\": [\n        \"Editor\"\n    ]"));
            Assert.That(File.Exists(Path.Combine(EmbeddedPackagePath, "Runtime/link.xml")), Is.False);
            Assert.That(File.Exists(Path.Combine(EmbeddedPackagePath, "Runtime/link.xml.meta")), Is.False);
            Assert.That(packageLock, Does.Contain("\"com.ivanmurzak.unity.mcp\": {\n      \"version\": \"file:com.ivanmurzak.unity.mcp\",\n      \"depth\": 0,\n      \"source\": \"embedded\""));
        }

        [Test]
        public void McpResolverAndNuGetImporters_AreEditorOnly()
        {
            var resolver = File.ReadAllText(Path.Combine(EmbeddedPackagePath, "Editor/DependencyResolver/NuGetConfig.cs"));
            var configurator = File.ReadAllText(Path.Combine(EmbeddedPackagePath, "Editor/DependencyResolver/NuGetPluginConfigurator.cs"));

            Assert.That(resolver, Does.Not.Contain("includeInBuild: true"));
            Assert.That(configurator, Does.Contain("Transitive dependency — default to editor-only."));
            Assert.That(configurator, Does.Contain("return false;"));
            Assert.That(configurator, Does.Contain("importer.SetExcludeEditorFromAnyPlatform(false);"));

            var importers = Directory.GetFiles("Assets/Plugins/NuGet", "*.dll.meta", SearchOption.TopDirectoryOnly);
            Assert.That(importers, Is.Not.Empty);
            foreach (var importerPath in importers)
            {
                var importer = File.ReadAllText(importerPath);
                Assert.That(importer, Does.Contain("Any:\n      enabled: 0"), importerPath);
                Assert.That(importer, Does.Contain("WebGL:\n      enabled: 0"), importerPath);
            }

            // Unity-provided BCL duplicates are intentionally disabled in the Editor,
            // while the actual MCP assemblies must remain available to editor tooling.
            AssertEditorEnabled("McpPlugin.dll.meta");
            AssertEditorEnabled("McpPlugin.Common.dll.meta");
        }

        private static void AssertEditorEnabled(string fileName)
        {
            var importer = File.ReadAllText(Path.Combine("Assets/Plugins/NuGet", fileName));
            Assert.That(importer, Does.Contain("Editor:\n      enabled: 1"), fileName);
        }
    }
}
