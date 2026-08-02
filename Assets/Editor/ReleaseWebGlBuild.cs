#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Reproducible WebGL release build entry point for Unity batch mode.
/// Invoke with -executeMethod ReleaseWebGlBuild.Build and provide
/// -releaseOutputPath &lt;directory&gt;.
/// </summary>
public static class ReleaseWebGlBuild
{
    private const string ReleaseOutputPathArgument = "-releaseOutputPath";
    private const string ResultFileName = "build-result.json";
    private const string McpRuntimeAsmdefRelativePath = "Packages/com.ivanmurzak.unity.mcp/Runtime/com.IvanMurzak.Unity.MCP.Runtime.asmdef";
    private const string McpTestFilesAsmdefRelativePath = "Packages/com.ivanmurzak.unity.mcp/TestFiles/com.IvanMurzak.Unity.MCP.TestFiles.asmdef";
    private const string McpRuntimeLinkXmlRelativePath = "Packages/com.ivanmurzak.unity.mcp/Runtime/link.xml";
    private const string McpRuntimeLinkXmlMetaRelativePath = "Packages/com.ivanmurzak.unity.mcp/Runtime/link.xml.meta";
    private const string NuGetPluginDirectoryRelativePath = "Assets/Plugins/NuGet";

    /// <summary>
    /// Builds all enabled scenes for WebGL and exits a batch-mode Unity process
    /// with 0 on success or 1 on a failed build/configuration error.
    /// </summary>
    public static void Build()
    {
        var exitCode = 1;
        string outputDirectory = null;
        string[] scenes = Array.Empty<string>();
        BuildReport report = null;

        try
        {
            outputDirectory = ReadOutputDirectory();
            scenes = GetEnabledScenes();
            ValidateMcpPlayerIsolation();
            Directory.CreateDirectory(outputDirectory);

            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"WebGL build completed with result {report.summary.result}.");

            WriteResult(outputDirectory, CreateResult(report, outputDirectory, scenes, null));
            exitCode = 0;
            Debug.Log($"WebGL release build succeeded: {outputDirectory}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (!string.IsNullOrEmpty(outputDirectory))
                TryWriteFailureResult(outputDirectory, report, scenes, exception);

            if (!Application.isBatchMode)
                throw;
        }
        finally
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }

    private static string ReadOutputDirectory()
    {
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 0; index < arguments.Length; index++)
        {
            if (!string.Equals(arguments[index], ReleaseOutputPathArgument, StringComparison.Ordinal))
                continue;

            if (index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                throw new ArgumentException($"{ReleaseOutputPathArgument} requires a directory path.");

            return Path.GetFullPath(arguments[index + 1]);
        }

        throw new ArgumentException($"Missing required Unity command-line argument: {ReleaseOutputPathArgument} <directory>.");
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
            throw new BuildFailedException("WebGL build requires at least one enabled scene in Build Settings.");

        return scenes.ToArray();
    }

    /// <summary>
    /// Rejects a release build if Unity-MCP or one of its NuGet dependencies can
    /// enter a player. The files are deliberately checked at build time: package
    /// updates and importer changes otherwise bypass source review.
    /// </summary>
    private static void ValidateMcpPlayerIsolation()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        RequireEditorOnlyAsmdef(projectRoot, McpRuntimeAsmdefRelativePath, "runtime");
        RequireEditorOnlyAsmdef(projectRoot, McpTestFilesAsmdefRelativePath, "test files");

        var runtimeLinkXmlPath = Path.Combine(projectRoot, McpRuntimeLinkXmlRelativePath);
        var runtimeLinkXmlMetaPath = Path.Combine(projectRoot, McpRuntimeLinkXmlMetaRelativePath);
        if (File.Exists(runtimeLinkXmlPath) || File.Exists(runtimeLinkXmlMetaPath))
            throw new BuildFailedException("Unity-MCP Runtime/link.xml and its metadata must not be present in a player-visible package path.");

        var nuGetPluginDirectory = Path.Combine(projectRoot, NuGetPluginDirectoryRelativePath);
        if (!Directory.Exists(nuGetPluginDirectory))
            throw new BuildFailedException($"Missing Unity-MCP NuGet plugin directory: {nuGetPluginDirectory}");

        var importerPaths = Directory.GetFiles(nuGetPluginDirectory, "*.dll.meta", SearchOption.TopDirectoryOnly);
        if (importerPaths.Length == 0)
            throw new BuildFailedException("Unity-MCP NuGet plugin directory contains no DLL importer metadata.");

        foreach (var importerPath in importerPaths)
        {
            var importer = File.ReadAllText(importerPath);
            if (!HasPlatformEnabled(importer, "Any", false)
                || !HasPlatformEnabled(importer, "WebGL", false))
            {
                throw new BuildFailedException(
                    $"Unity-MCP NuGet DLL must be excluded from player platforms and WebGL: {importerPath}");
            }
        }
    }

    private static void RequireEditorOnlyAsmdef(string projectRoot, string relativePath, string description)
    {
        var asmdefPath = Path.Combine(projectRoot, relativePath);
        if (!File.Exists(asmdefPath))
            throw new BuildFailedException($"Missing embedded Unity-MCP {description} assembly definition: {asmdefPath}");

        var asmdef = File.ReadAllText(asmdefPath);
        if (!asmdef.Contains("\"includePlatforms\": [\n        \"Editor\"\n    ]"))
            throw new BuildFailedException($"Unity-MCP {description} assembly must be limited to the Editor platform.");
    }

    private static bool HasPlatformEnabled(string importerYaml, string platform, bool enabled)
    {
        var expectedBlock = $"\n    {platform}:\n      enabled: {(enabled ? 1 : 0)}\n";
        return importerYaml.Contains(expectedBlock);
    }

    private static void TryWriteFailureResult(string outputDirectory, BuildReport report, string[] scenes, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            WriteResult(outputDirectory, CreateResult(report, outputDirectory, scenes, exception));
        }
        catch (Exception resultException)
        {
            Debug.LogError($"Could not write {ResultFileName}: {resultException}");
        }
    }

    private static BuildResultRecord CreateResult(BuildReport report, string outputDirectory, string[] scenes, Exception exception)
    {
        var hasReport = report != null;
        var summary = hasReport ? report.summary : default(BuildSummary);
        return new BuildResultRecord
        {
            status = exception == null && hasReport && summary.result == BuildResult.Succeeded ? "succeeded" : "failed",
            outputDirectory = outputDirectory,
            unityVersion = Application.unityVersion,
            timestampUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            scenes = scenes,
            buildResult = hasReport ? summary.result.ToString() : "not-started",
            totalSizeBytes = hasReport ? summary.totalSize : 0,
            totalErrors = hasReport ? summary.totalErrors : 0,
            totalWarnings = hasReport ? summary.totalWarnings : 0,
            error = exception == null ? null : exception.ToString()
        };
    }

    private static void WriteResult(string outputDirectory, BuildResultRecord result)
    {
        var resultPath = Path.Combine(outputDirectory, ResultFileName);
        File.WriteAllText(resultPath, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
        Debug.Log($"WebGL release build result: {resultPath}");
    }

    [Serializable]
    private sealed class BuildResultRecord
    {
        public string status;
        public string outputDirectory;
        public string unityVersion;
        public string timestampUtc;
        public string[] scenes;
        public string buildResult;
        public ulong totalSizeBytes;
        public int totalErrors;
        public int totalWarnings;
        public string error;
    }
}
#endif
