# Changelog

## 0.3.0

- Added UPM package metadata so `Assets/CAudio` can be installed through Package Manager with a Git URL path.
- Added an importable `Feature Showcase` UPM sample with a uGUI control scene and bundled sample audio.
- Added runtime assembly definitions for Runtime, Editor, and EditMode tests.
- Added safer `AudioPlayOptions` cloning to avoid mutating caller-owned options.
- Added EditMode tests for options, cue selection, database validation, failure results, cooldown, max simultaneous playback, async requests, and group stop.
- Added async playback requests and optional Addressables provider support.
- Added music crossfade, music queue, Mixer parameter transitions, Snapshot transitions, channel pause/resume, mute, solo, key-prefix stop, and group stop.
- Added cue cooldown, max simultaneous playback, group, sequential selection, shuffle selection, and no-immediate-repeat selection.
- Improved the audio database editor window with filtering, issue filtering, duplication, delete confirmation, selected-clip import, drag-and-drop import, validation locating, and a custom `AudioCueData` drawer.
- Fixed paused non-looping audio being treated as finished.
- Fixed music queues so failed entries are skipped and later entries can continue.
- Fixed fade-out volume so channel/master volume is not applied twice.
- Added audio source pool limits and `PoolLimitReached` playback failure results.
- Added optional clip release lifecycle support for Addressables and custom providers.
- Improved validation issue metadata and editor-side issue locating.

## 0.2.0

- Added independent `AudioCueAsset` support.
- Added database validation, debug settings, Mixer parameter mapping, Voice Ducking, and additional playback helpers.

## 0.1.0

- Added initial lightweight runtime audio system, database, cue model, source pool, playback handles, and basic playback API.
