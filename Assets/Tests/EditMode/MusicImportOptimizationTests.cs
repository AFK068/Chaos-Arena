using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ChaosArena.Platform.Tests
{
    public sealed class MusicImportOptimizationTests
    {
        private const string MusicFolder = "Assets/Resources/Audio/MusicSrc";

        [Test]
        public void MusicClips_StayCompressedAtReleaseQuality()
        {
            var clipPaths = Directory.GetFiles(MusicFolder, "*.wav", SearchOption.TopDirectoryOnly);
            Assert.That(clipPaths, Is.Not.Empty);

            foreach (var clipPath in clipPaths)
            {
                var importer = AssetImporter.GetAtPath(clipPath.Replace('\\', '/')) as AudioImporter;
                Assert.That(importer, Is.Not.Null, clipPath);

                var settings = importer!.defaultSampleSettings;
                Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory), clipPath);
                Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), clipPath);
                Assert.That(settings.quality, Is.EqualTo(0.6f).Within(0.001f), clipPath);
            }
        }
    }
}
