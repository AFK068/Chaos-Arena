# SFX import and assignment

`Assets/Resources/Audio/SfxLibrary.asset` intentionally contains no audio files. It is a
safe, versioned list of the sounds expected by gameplay: shoot, dash, player hit/death,
enemy hit/death, coin, chest, and UI click. Add only clips whose licence permits use in
Chaos Arena and record the source, licence, author, and any attribution requirement in the
asset delivery or project licence inventory.

## Assigning a licensed pack

1. Place the approved source files under `Assets/Audio/SFX/<pack-name>/`. Do not put an
   unverified download under `Resources`.
2. Select `Resources/Audio/SfxLibrary` and populate each matching slot's `clips` array.
   Multiple clips in a slot are varied without an immediate repeat.
3. Tune each slot's volume, pitch range, and `minInterval`. Hits, shots, and coins already
   have conservative anti-spam defaults. An empty slot is a deliberate, silent no-op.
4. Route imported clips through the existing `SFX` group in `Resources/Audio/MainMixer`.
   Its exposed volume parameter is `SFXVolume` (case-sensitive).
5. For a Unity UI Button, add `AudioManager.Instance.PlaySfx(SfxCue.UiClick)` from the
   owning UI handler after the action succeeds. This foundation deliberately leaves
   localization, Yandex, and mobile UI wiring untouched.

## Import settings

- Use mono for ordinary 2D effects. Keep stereo only where the clip needs it.
- Enable `Load In Background`; use `Decompress On Load` for short, latency-sensitive clips
  such as shoot, dash, and UI click. Use `Compressed In Memory` for longer one-shot deaths.
- Prefer Vorbis for WebGL and mobile builds; test quality around 60-80% and avoid extremely
  short encoded clips that create audible codec artifacts.
- Keep `Preload Audio Data` enabled for gameplay-critical short effects; leave it disabled
  only for rare, non-critical effects after profiling.
- Verify the resulting WebGL build in-browser and an actual mobile device. Import mode,
  sample rate, and browser memory behavior are platform-dependent.

No third-party clip is added by this change. The asset pack owner must provide the approved
path and licence/attribution evidence before any slot is populated.
