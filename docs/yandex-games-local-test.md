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

Localization is the next stage. The bridge already exposes the startup
`environment.i18n.lang` value as `YandexPlatformService.LanguageCode`; no UI
translation tables or language selection have been implemented yet.

Official references:

- https://yandex.ru/dev/games/doc/ru/concepts/local-launch
- https://yandex.ru/dev/games/doc/ru/sdk/sdk-about
- https://yandex.ru/dev/games/doc/ru/console/debug-panel
