# Reproducible WebGL release build

From the repository root on macOS, run Unity 6000.3.1f1 in batch mode:

```sh
"/Applications/Unity/Hub/Editor/6000.3.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -nographics \
  -projectPath "$PWD" \
  -executeMethod ReleaseWebGlBuild.Build \
  -releaseOutputPath "/private/tmp/chaos-arena-webgl-release" \
  -logFile "$PWD/Logs/webgl-release-build.log"
```

`-releaseOutputPath` is required and is the WebGL build directory. The builder
uses only enabled Build Settings scenes, targets `BuildTarget.WebGL`, and uses
Unity `StrictMode`. It does not edit scenes or Build Settings.

The process exits with code `0` only when Unity reports `Succeeded`; missing
arguments, no enabled scenes, exceptions, and any other build result exit with
code `1`. It writes `<releaseOutputPath>/build-result.json` after every build
attempt for which the output directory can be resolved. The JSON records the
status, Unity version, enabled scenes, Unity build result, summary counts, and
any exception text.

Use a clean, disposable output directory for each release candidate. The
builder deliberately does not delete an existing output directory.
