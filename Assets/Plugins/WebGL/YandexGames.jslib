mergeInto(LibraryManager.library, {
  YandexGames_Initialize: function (gameObjectNamePointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    bridge.gameObjectName = UTF8ToString(gameObjectNamePointer);

    function send(method, value) {
      if (typeof SendMessage === 'function') {
        SendMessage(bridge.gameObjectName, method, value || '');
      }
    }

    function detectBrowserDeviceType() {
      if (navigator.userAgentData && navigator.userAgentData.mobile === true) {
        return 'mobile';
      }

      return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent || '')
        ? 'mobile'
        : 'desktop';
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
        bridge.deviceType = sdk.deviceInfo && sdk.deviceInfo.type
          ? sdk.deviceInfo.type
          : detectBrowserDeviceType();

        sdk.on('game_api_pause', function () { send('OnYandexGamesPlatformPause'); });
        sdk.on('game_api_resume', function () { send('OnYandexGamesPlatformResume'); });
        send('OnYandexGamesDeviceTypeDetected', bridge.deviceType);
        send('OnYandexGamesInitialized', bridge.language);
      })
      .catch(function (error) {
        var text = error && error.message ? error.message : String(error);
        send('OnYandexGamesDeviceTypeDetected', detectBrowserDeviceType());
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
  },

  YandexGames_SetFullscreenAdReceiver: function (gameObjectNamePointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    bridge.fullscreenAdReceiverName = UTF8ToString(gameObjectNamePointer);
  },

  YandexGames_ShowFullscreenAdv: function (requestIdPointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    var requestId = UTF8ToString(requestIdPointer);
    var completed = false;

    function sendTerminal(result) {
      if (completed) {
        return;
      }
      completed = true;

      if (typeof SendMessage === 'function' && bridge.fullscreenAdReceiverName) {
        // JSON.stringify is required because SendMessage accepts a string payload.
        SendMessage(
          bridge.fullscreenAdReceiverName,
          'OnYandexGamesFullscreenAdTerminal',
          JSON.stringify({ requestId: requestId, result: result }));
      }
    }

    var sdk = bridge.sdk;
    if (!sdk || !sdk.adv || typeof sdk.adv.showFullscreenAdv !== 'function') {
      sendTerminal('unavailable');
      return;
    }

    try {
      var result = sdk.adv.showFullscreenAdv({
        callbacks: {
          onOpen: function () {},
          onClose: function (wasShown) { sendTerminal('closed'); },
          onError: function () { sendTerminal('error'); }
        }
      });

      // Some SDK implementations surface failures as a rejected promise in
      // addition to the callbacks. Both paths share the terminal guard.
      if (result && typeof result.catch === 'function') {
        result.catch(function () { sendTerminal('error'); });
      }
    } catch (error) {
      sendTerminal('error');
    }
  },

  YandexGames_SetPlayerReceiver: function (gameObjectNamePointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    bridge.playerReceiverName = UTF8ToString(gameObjectNamePointer);
  },

  YandexGames_PlayerGetGuest: function (requestIdPointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    var requestId = UTF8ToString(requestIdPointer);
    var completed = false;

    function sendTerminal(result, data) {
      if (completed) {
        return;
      }
      completed = true;
      if (typeof SendMessage === 'function' && bridge.playerReceiverName) {
        // JSON.stringify escapes request ids and returned document text before it
        // crosses the SendMessage string boundary.
        SendMessage(bridge.playerReceiverName, 'OnYandexGamesPlayerTerminal', JSON.stringify({
          requestId: requestId,
          operation: 'getPlayer',
          result: result,
          data: data || ''
        }));
      }
    }

    if (!bridge.sdk || typeof bridge.sdk.getPlayer !== 'function') {
      sendTerminal('error', '');
      return;
    }

    try {
      // Explicitly suppress scope prompts for passive guest/bootstrap access.
      Promise.resolve(bridge.sdk.getPlayer({ scopes: false }))
        .then(function (player) {
          bridge.player = player;
          sendTerminal('ok', '');
        })
        .catch(function () { sendTerminal('error', ''); });
    } catch (error) {
      sendTerminal('error', '');
    }
  },

  YandexGames_PlayerGetData: function (requestIdPointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    var requestId = UTF8ToString(requestIdPointer);
    var completed = false;
    var progressKey = 'chaos_arena.progress.v1';

    function sendTerminal(result, data) {
      if (completed) {
        return;
      }
      completed = true;
      if (typeof SendMessage === 'function' && bridge.playerReceiverName) {
        SendMessage(bridge.playerReceiverName, 'OnYandexGamesPlayerTerminal', JSON.stringify({
          requestId: requestId,
          operation: 'getData',
          result: result,
          data: data || ''
        }));
      }
    }

    var player = bridge.player;
    if (!player || typeof player.getData !== 'function') {
      sendTerminal('error', '');
      return;
    }

    try {
      Promise.resolve(player.getData([progressKey]))
        .then(function (data) {
          var serialized = '';
          try {
            if (data && data[progressKey] !== undefined && data[progressKey] !== null) {
              serialized = JSON.stringify(data[progressKey]);
            }
          } catch (error) {
            sendTerminal('error', '');
            return;
          }
          sendTerminal('ok', serialized);
        })
        .catch(function () { sendTerminal('error', ''); });
    } catch (error) {
      sendTerminal('error', '');
    }
  },

  YandexGames_PlayerSetData: function (requestIdPointer, serializedDocumentPointer) {
    var bridge = window.YandexGamesUnityBridge = window.YandexGamesUnityBridge || {};
    var requestId = UTF8ToString(requestIdPointer);
    var serializedDocument = UTF8ToString(serializedDocumentPointer);
    var completed = false;
    var progressKey = 'chaos_arena.progress.v1';

    function sendTerminal(result) {
      if (completed) {
        return;
      }
      completed = true;
      if (typeof SendMessage === 'function' && bridge.playerReceiverName) {
        SendMessage(bridge.playerReceiverName, 'OnYandexGamesPlayerTerminal', JSON.stringify({
          requestId: requestId,
          operation: 'setData',
          result: result,
          data: ''
        }));
      }
    }

    var player = bridge.player;
    if (!player || typeof player.setData !== 'function') {
      sendTerminal('error');
      return;
    }

    try {
      var document = JSON.parse(serializedDocument);
      var data = {};
      data[progressKey] = document;
      Promise.resolve(player.setData(data, true))
        .then(function () { sendTerminal('ok'); })
        .catch(function () { sendTerminal('error'); });
    } catch (error) {
      sendTerminal('error');
    }
  }
});
