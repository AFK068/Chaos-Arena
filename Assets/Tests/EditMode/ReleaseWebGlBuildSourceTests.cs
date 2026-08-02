using System.IO;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class ReleaseWebGlBuildSourceTests
    {
        [Test]
        public void ReleaseBuilder_UsesExplicitOutputEnabledScenesAndStrictWebGlBuild()
        {
            var source = File.ReadAllText("Assets/Editor/ReleaseWebGlBuild.cs");

            Assert.That(source, Does.Contain("-releaseOutputPath"));
            Assert.That(source, Does.Contain("EditorBuildSettings.scenes"));
            Assert.That(source, Does.Contain("BuildPipeline.BuildPlayer"));
            Assert.That(source, Does.Contain("BuildTarget.WebGL"));
            Assert.That(source, Does.Contain("BuildOptions.StrictMode"));
        }

        [Test]
        public void ReleaseBuilder_WritesMachineReadableResultAndExitsBatchMode()
        {
            var source = File.ReadAllText("Assets/Editor/ReleaseWebGlBuild.cs");

            Assert.That(source, Does.Contain("build-result.json"));
            Assert.That(source, Does.Contain("JsonUtility.ToJson"));
            Assert.That(source, Does.Contain("EditorApplication.Exit(exitCode)"));
        }
    }
}
