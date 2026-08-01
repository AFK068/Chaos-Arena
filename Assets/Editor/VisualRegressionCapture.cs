#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ChaosArena.Platform;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch-mode visual smoke for MainMenu.  It never opens GameView or a player
/// window: Play Mode renders each localized canvas into a 1920x1080
/// RenderTexture and writes PNGs outside Assets.
/// </summary>
[InitializeOnLoad]
public static class VisualRegressionCapture
{
    private const string OutputDirectory = "/private/tmp/chaos-arena-tmp-render-layout";
    private const string PendingKey = "ChaosArena.OffscreenVisualCapture.Pending";
    private const string LanguageIndexKey = "ChaosArena.OffscreenVisualCapture.LanguageIndex";
    private const string ExitAfterPlayModeKey = "ChaosArena.OffscreenVisualCapture.ExitAfterPlayMode";
    private static readonly string[] Languages =
    {
        LocalizationLanguagePolicy.English,
        LocalizationLanguagePolicy.Russian,
        LocalizationLanguagePolicy.Turkish
    };

    private static int _framesUntilCapture;
    private static bool _languageApplied;

    static VisualRegressionCapture()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterDomainReload;
    }

    /// <summary>
    /// Entry point for <c>-batchmode -executeMethod
    /// VisualRegressionCapture.CaptureMainMenuOffscreen</c>.
    /// </summary>
    public static void CaptureMainMenuOffscreen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Close Play Mode before capturing visual regression PNGs.");

        Directory.CreateDirectory(OutputDirectory);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        SessionState.SetInt(LanguageIndexKey, 0);
        SessionState.SetBool(ExitAfterPlayModeKey, false);
        _framesUntilCapture = 0;
        _languageApplied = false;
        EditorApplication.delayCall += ResumeAfterDomainReload;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            EditorApplication.update -= AdvanceCapture;
            EditorApplication.update += AdvanceCapture;
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ExitAfterPlayModeKey, false))
        {
            SessionState.SetBool(ExitAfterPlayModeKey, false);
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }

    private static void ResumeAfterDomainReload()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;

        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= AdvanceCapture;
            EditorApplication.update += AdvanceCapture;
        }
        else if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.EnterPlaymode();
        }
    }

    private static void AdvanceCapture()
    {
        var localization = LocalizationService.Instance;
        if (localization == null)
            return;

        if (!_languageApplied)
        {
            localization.SetManualLanguage(Languages[SessionState.GetInt(LanguageIndexKey, 0)]);
            _framesUntilCapture = 12;
            _languageApplied = true;
            return;
        }

        if (_framesUntilCapture-- > 0)
            return;

        var languageIndex = SessionState.GetInt(LanguageIndexKey, 0);
        RenderMenuToPng(Path.Combine(OutputDirectory, $"main-menu-{Languages[languageIndex]}-offscreen.png"));

        var nextLanguageIndex = languageIndex + 1;
        SessionState.SetInt(LanguageIndexKey, nextLanguageIndex);
        if (nextLanguageIndex < Languages.Length)
        {
            _framesUntilCapture = 0;
            _languageApplied = false;
            return;
        }

        EditorApplication.update -= AdvanceCapture;
        SessionState.SetBool(PendingKey, false);
        SessionState.SetBool(ExitAfterPlayModeKey, true);
        EditorApplication.ExitPlaymode();
    }

    private static void RenderMenuToPng(string outputPath)
    {
        var cameraObject = new GameObject("Offscreen Visual Regression Camera");
        var camera = cameraObject.AddComponent<Camera>();
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var previousTarget = RenderTexture.active;
        var states = new List<CanvasState>();

        try
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = ~0;
            camera.targetTexture = target;
            target.Create();

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                states.Add(new CanvasState(canvas));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
        finally
        {
            foreach (var state in states)
                state.Restore();
            RenderTexture.active = previousTarget;
            target.Release();
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    private readonly struct CanvasState
    {
        private readonly Canvas _canvas;
        private readonly RenderMode _renderMode;
        private readonly Camera _worldCamera;
        private readonly float _planeDistance;

        public CanvasState(Canvas canvas)
        {
            _canvas = canvas;
            _renderMode = canvas.renderMode;
            _worldCamera = canvas.worldCamera;
            _planeDistance = canvas.planeDistance;
        }

        public void Restore()
        {
            if (_canvas == null)
                return;
            _canvas.renderMode = _renderMode;
            _canvas.worldCamera = _worldCamera;
            _canvas.planeDistance = _planeDistance;
        }
    }
}
#endif
