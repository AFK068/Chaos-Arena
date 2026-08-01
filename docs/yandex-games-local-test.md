# Yandex Games: local WebGL test

This project dynamically loads `/sdk.js` in its WebGL bridge and then calls
`YaGames.init()`. Do not download or commit `sdk.js`.

After producing a WebGL build (for example, `Build/WebGL`), run the official
development proxy from the project root:

```sh
npx @yandex-games/sdk-dev-proxy -p Build/WebGL --dev-mode=true
```

The proxy provides local SDK mocks and logs SDK calls in the browser console.
To test a draft against the platform, add its ID:

```sh
npx @yandex-games/sdk-dev-proxy -p Build/WebGL --app-id=<DRAFT_ID>
```

Use the Yandex Games debug panel (`debug-mode=16`) to check the current loader,
Game Ready, gameplay pause/resume, and focus-loss behavior. The platform
service is deliberately fail-open: when SDK loading or initialization fails,
the Unity menu and game remain usable, but no Yandex lifecycle calls are sent.

The bridge exposes the startup
`environment.i18n.lang` value as `YandexPlatformService.LanguageCode`.

## RU/EN localization foundation

`LocalizationService` is created before scenes. In automatic mode it maps SDK
languages `ru`, `be`, `kk`, `uk`, and `uz` to Russian; all other values use
English. A manual choice takes priority after restart until `ResetToAuto()` or
`UsePlatformLanguage()` is called.

To localize an existing TextMeshPro label in the Unity Editor:

1. Add `LocalizedText` to the same GameObject as its `TMP_Text`.
2. Set **Key** to one of the exact keys below. Leave **Target Text** blank to
   use the local `TMP_Text` automatically.
3. Add `LanguageToggleUI` to an existing button object and assign its TMP label.
   Bind the Button `OnClick` event to `ToggleLanguage()`. The label displays
   `RU` or `EN`. Bind `UsePlatformLanguage()` when an explicit Auto option is
   needed.

| UI text | Key |
| --- | --- |
| New Run | `menu.new_run` |
| Settings | `menu.settings` |
| Quit | `menu.quit` |
| Best floor | `stats.best_floor` |
| Deaths | `stats.deaths` |
| Total coins | `stats.total_coins` |
| Kills | `stats.kills` |
| Sounds | `settings.sounds` |
| Music | `settings.music` |
| Return | `settings.return` |
| SETTINGS | `settings.title` |
| PAUSED | `pause.title` |
| Continue | `pause.continue` |
| GAME OVER | `game_over.title` |
| Enemies slain | `game_over.enemies_slain` |
| Coins collected | `game_over.coins_collected` |
| Run time | `game_over.run_time` |
| Floor reached | `game_over.floor_reached` |
| Main Menu | `game_over.main_menu` |
| FLOOR {0} | `transition.floor` |
| Item | `common.item` |

`CHAOS ARENA` deliberately has no key and remains unchanged in every language.

Official references:

- https://yandex.ru/dev/games/doc/ru/concepts/local-launch
- https://yandex.ru/dev/games/doc/ru/sdk/sdk-about
- https://yandex.ru/dev/games/doc/ru/console/debug-panel
