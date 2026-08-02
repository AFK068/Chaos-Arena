# Localization and first-room tutorial QA

## Automated coverage

`LocalizationFoundationTests` checks SDK routing, persisted-manual selection semantics, the `EN -> RU -> TR -> EN` cycle, catalog coverage for all three languages, the exact 24 `ProximityLabel` prefab keys, localization ownership for static text in the three release scenes, and the floor-1/node-0 tutorial gate.

## Manual Unity / WebGL check

1. Start a new run in English, Russian, then Turkish. In Settings, use the language control repeatedly: it must show `EN`, `RU`, `TR`, then `EN`; quit/reload and confirm the manual selection persists. Reset to auto and verify SDK `tr` selects Turkish, `ru/be/kk/uk/uz` Russian, and another code English.
2. Visit every heart, projectile pickup, item, and trader. The existing English `labelText` must remain serialized as fallback, while the visible tooltip changes live when language is switched. Check the longest Russian/Turkish descriptions on a compact landscape viewport: text wraps inside the tooltip instead of extending off-screen.
3. Begin each new run. On floor 1, node 0, an inert world-space controls plaque appears near the lower part of the start room. It must say exactly: desktop `WASD` move, arrows aim/shoot, `Shift` dash, `E / F` use; touch builds must instead show one move stick, auto aim/fire, Dash, and Use. It must not mention mouse fire.
4. Change language while the plaque is visible; text updates immediately. Open pause/settings: plaque hides; resume: it returns. Leave the start room, advance floor, restart, and leave Gameplay: it is removed. A restarted run shows it again only in the new floor-1 start room.
5. Verify the plaque has no collider and does not consume clicks/touches, and that it remains behind the normal pause/settings UI. Confirm the `Press Start 2P Font` resource is used when present, otherwise `BoldPixels Font` is used.
6. On the main menu, switch between EN/RU/TR and confirm the title is respectively `Chaos Arena`, `Арена Хаоса`, and `Kaos Arenası`. The Russian branding deliberately has no English words.
7. Collect Rage and Dash Charge. Each must confirm the effect with a brief localized notification after it is actually collected. They intentionally do not use the long proximity tooltips used for interactable content: both pickups are automatic and a tooltip could disappear before it can be read.

## Scope notes

Price and the `II` pause glyph deliberately remain neutral. The title is player-facing localized copy: `Chaos Arena` / `Арена Хаоса` / `Kaos Arenası`. This change does not rename prefabs or GameObjects, so inventory/loot identifiers stay stable.

## One-hand mobile overlay QA

The touch overlay exists only during landscape gameplay. It has one move stick plus Dash, Use, Pause and an explicit hand selector in the same safe-area cluster. The selector begins at `RIGHT HAND` by default and toggles the entire cluster to the left safe-area edge; the selected side is persisted in `ChaosArena.Mobile.Hand`. Verify it in EN/RU/TR, then rotate portrait: the overlay, movement, auto aim and fire must all stop while the existing rotate-platform behavior remains visible. Rotate back, pause/open settings/resume, change floor and return to the main menu; no touch overlay or residual firing may remain outside active landscape gameplay. On a compact 844x390 landscape viewport, verify the 144-unit action buttons and 112-unit Pause/Hand buttons remain at least 44 CSS pixels after CanvasScaler.
