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
