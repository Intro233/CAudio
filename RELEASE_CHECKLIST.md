# CAudio Release Checklist

This checklist tracks the evidence required before marking the full iteration goal complete.

## Required Verification

- Unity Console has no C# compile errors.
- EditMode tests pass and produce `TestResults.xml`.
- `git diff --check` passes.
- README, quick start, changelog, version file, samples, runtime/editor/test assembly definitions are present.
- `Assets/CAudio/package.json`, package README, package changelog, and package license are present.
- Editor window can be opened without GUILayout state errors.

## Verification Commands

Close any Unity Editor instance that has this project open, then run. Do not pass `-quit` with this Unity Test Framework version; the command-line test starter exits Unity after the run finishes.

```powershell
& "D:\Program\Unity\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "D:\Program\Unity\Projects\CAudio" -runTests -testPlatform EditMode -testResults "D:\Program\Unity\Projects\CAudio\TestResults.xml" -logFile "D:\Program\Unity\Projects\CAudio\UnityTest.log"
```

Static check:

```powershell
git diff --check
```

UPM package metadata check:

```powershell
Get-Content "Assets\CAudio\package.json" -Raw | ConvertFrom-Json
```

Package Manager Git URL format:

```text
https://github.com/<user>/<repo>.git?path=/Assets/CAudio#master
```

## Iteration 3 Evidence

- Encoding and text: source files, `README.md`, `QUICK_START.md`, and editor UI text read as UTF-8 Chinese.
- Options safety: `AudioPlayOptions.Clone()` and `AudioPlayOptions.CopyOrDefault()` prevent playback helpers from mutating caller-owned options.
- Lifecycle hardening: `AudioSourcePool` resets reused sources; stop fade avoids repeated long-fade resets.
- Tests: EditMode tests cover options, cue selection, database validation, failure paths, cooldown, max simultaneous playback, async requests, and group stop.
- Documentation: `QUICK_START.md`.

## Iteration 4 Evidence

- `AudioDatabaseWindow` supports channel filtering, issue filtering, duplicate, delete confirmation, selected-clip import, validation display, and database statistics.
- `AudioCueDataDrawer` gives Cue fields a stable editor layout.
- `CAudio.Editor.asmdef` separates editor code from runtime code.

## Iteration 5 Evidence

- `AudioAsyncPlayRequest`.
- `AudioService.PlayAsync` and `AudioManager.PlayAsync`.
- `AddressablesAudioClipProvider` is guarded by `CAUDIO_ADDRESSABLES`.
- `CAudio.Runtime.asmdef` uses `versionDefines` so Addressables remains optional.

## Iteration 6 Evidence

- Music crossfade.
- Music queue.
- Mixer parameter transitions.
- Snapshot transitions.
- Channel pause/resume.
- Mute and solo.
- Cue cooldown.
- Max simultaneous playback.
- Selection modes: random, weighted random, sequential, shuffle, no immediate repeat.
- Stop by key, key prefix, channel, group, handle, and all.

## Iteration 7 Evidence

- `README.md`.
- `QUICK_START.md`.
- `CHANGELOG.md`.
- `VERSION`.
- Runtime, Editor, and EditMode test asmdefs.
- `Assets/CAudio/Samples` with `CAudioSampleController`.

## Package Evidence

- `Assets/CAudio/package.json` defines `com.caudio.core` version `0.3.0`.
- `Assets/CAudio/README.md`, `CHANGELOG.md`, `LICENSE.md`, and `Documentation~/index.md` are present.
- The package can be installed from a Git URL using `?path=/Assets/CAudio#master`.
- `Assets/CAudio/package.json` registers the `Feature Showcase` importable sample.
- Addressables remains optional through `CAudio.Runtime.asmdef` `versionDefines`.

## Latest Verification

- `git diff --check` passes.
- A temporary project copy at `C:\Users\Nagisa\AppData\Local\Temp\CAudio_FourBatchTest_20260619_081744` compiled Runtime, Editor, Samples, and EditMode Tests assemblies successfully.
- EditMode tests passed in the temporary project copy after removing `-quit`: `TestResults_EditMode.xml` reports 28 total, 28 passed, 0 failed, 0 skipped.

## Current Known Blocker

As of the latest check, Unity processes are still running with this project open. Unity rejects batchmode testing while the same project is already open, so `TestResults.xml` cannot be produced in the original project path until those editors are closed. The temporary project copy provides current compile and EditMode test evidence.

Latest observed rejection:

```text
Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.
Multiple Unity instances cannot open the same project.
Project: D:/Program/Unity/Projects/CAudio
```
