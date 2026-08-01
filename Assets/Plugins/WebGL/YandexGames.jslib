mergeInto(LibraryManager.library, {
  YandexGames_Initialize: function (gameObjectNamePointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    bridge.gameObjectName = UTF8ToString(gameObjectNamePointer);

    function send(method, value) {
      if (typeof SendMessage === 'function') {
        SendMessage(bridge.gameObjectName, method, value || '');
      }
    }

    function loadSdk() {
      if (window.YaGames) {
        return Promise.resolve();
      }

      if (bridge.sdkScriptPromise) {
        return bridge.sdkScriptPromise;
      }

      bridge.sdkScriptPromise = new Promise(function (resolve, reject) {
        var script = document.createElement('script');
        script.async = true;
        script.src = '/sdk.js';
        script.onload = resolve;
        script.onerror = function () { reject(new Error('Unable to load /sdk.js')); };
        document.head.appendChild(script);
      });

      return bridge.sdkScriptPromise;
    }

    loadSdk()
      .then(function () { return window.YaGames.init(); })
      .then(function (sdk) {
        bridge.sdk = sdk;
        bridge.language = sdk.environment && sdk.environment.i18n
          ? sdk.environment.i18n.lang
          : 'en';

        sdk.on('game_api_pause', function () { send('OnYandexGamesPlatformPause'); });
        sdk.on('game_api_resume', function () { send('OnYandexGamesPlatformResume'); });
        send('OnYandexGamesInitialized', bridge.language);
      })
      .catch(function (error) {
        var text = error && error.message ? error.message : String(error);
        send('OnYandexGamesError', text);
      });
  },

  YandexGames_LoadingReady: function () {
    var sdk = window.YandexGamesUnityBridge && window.YandexGamesUnityBridge.sdk;
    if (sdk && sdk.features && sdk.features.LoadingAPI) {
      sdk.features.LoadingAPI.ready();
    }
  },

  YandexGames_GameplayStart: function () {
    var sdk = window.YandexGamesUnityBridge && window.YandexGamesUnityBridge.sdk;
    if (sdk && sdk.features && sdk.features.GameplayAPI) {
      sdk.features.GameplayAPI.start();
    }
  },

  YandexGames_GameplayStop: function () {
    var sdk = window.YandexGamesUnityBridge && window.YandexGamesUnityBridge.sdk;
    if (sdk && sdk.features && sdk.features.GameplayAPI) {
      sdk.features.GameplayAPI.stop();
    }
  }
});
