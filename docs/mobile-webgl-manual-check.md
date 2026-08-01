# Mobile WebGL controls: manual check

The controls are created at runtime only in `Gameplay` when Unity reports a
mobile platform, a handheld device, or a `Touchscreen`. Desktop keyboard,
mouse and gamepad paths remain driven by `PlayerInputActions`. Button labels
come from the existing RU/EN catalog and refresh on a language change.

The overlay uses a 1920x1080 reference canvas with `matchWidthOrHeight = 0.5`.
At a compact 844x390 landscape viewport this is about a 0.40 scale, so the
260-unit sticks are about 104 CSS pixels and 144-unit action buttons about 57
CSS pixels. The pause button is 112 units, or about 44.6 CSS pixels at that
viewport. This keeps every actionable target at or above the usual 44px floor
without moving the controls into the top HUD.

1. In Unity, open `Assets/Scenes/MainMenu.unity`, `Gameplay.unity`, and
   `GameOver.unity`. Confirm the EventSystem has `InputSystemUiModuleBinder`
   and no missing Input Action references.
2. Run `Gameplay` in a landscape device simulator or a mobile WebGL build.
   Two sticks should appear inside the device safe area: move on the lower
   left, aim-and-fire on the lower right. The right stick fires continuously
   while held, and stops when released.
3. Check the localized Dash and Use buttons above the sticks; Dash uses the
   current move direction and Use performs the same nearby interaction as `F`.
   The 112-unit `II` button above the right controls must open and close the
   existing pause menu. Check it can be pressed reliably around its full edge,
   not only at the centre.
4. Rotate a notched device or change its safe area. Controls must stay inside
   the safe area and leave the top HUD/minimap unobstructed. In portrait the
   overlay is intentionally hidden; this MVP targets landscape.
5. Repeat in an editor/desktop WebGL browser without touch. The overlay must
   be absent; WASD, arrows, Shift, F, Escape, mouse UI and menu buttons must
   still work.
6. Open pause and Settings from a touch device. Gameplay controls must hide
   below the pause/settings UI, remain hidden while local pause is active, and
   return after Resume. A platform-only pause must keep its existing platform
   behavior; this change does not take ownership of it.

Before release, run the target Unity version's EditMode suite and make an
actual mobile browser pass on the intended Yandex WebGL host. The desktop
Editor cannot prove mobile browser touch delivery or device safe-area values.
