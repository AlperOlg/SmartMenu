/**
 * Smart QR Menu — merkezi ses yöneticisi (Howler.js)
 * Kullanım:
 *   SoundManager.play('success' | 'success1' | 'success2' | 'orderSuccess' | 'error' | 'aiMessage')
 *   SoundManager.playAfterRedirect('success2')  // full-page POST/redirect sonrası çal
 */
(function (global) {
  "use strict";

  var PENDING_KEY = "pendingSound";

  var SOUND_DEFS = {
    success: { src: ["/sounds/success.mp3"], volume: 0.5 },
    success1: { src: ["/sounds/success1.mp3"], volume: 0.55 },
    success2: { src: ["/sounds/success2.mp3"], volume: 0.45 },
    orderSuccess: { src: ["/sounds/order-success.mp3"], volume: 0.55 },
    error: { src: ["/sounds/error.mp3"], volume: 0.4 },
    aiMessage: { src: ["/sounds/ai-message.mp3"], volume: 0.4 },
  };

  var sounds = Object.create(null);
  var unlocked = false;

  function createHowl(name, def) {
    if (typeof Howl === "undefined") {
      return null;
    }

    try {
      return new Howl({
        src: def.src,
        volume: def.volume,
        preload: true,
        html5: false,
        onloaderror: function () {
          // Eksik / bozuk dosya olursa sessizce yoksay
          sounds[name] = null;
        },
        onplayerror: function (id, err) {
          // Tarayıcı autoplay kısıtı kullanıcı etkileşiminde unlock
          var howl = sounds[name];
          if (!howl) return;
          howl.once("unlock", function () {
            try {
              howl.play();
            } catch (e) {
              /* ignore */
            }
          });
        },
      });
    } catch (e) {
      return null;
    }
  }

  function ensureSounds() {
    if (typeof Howl === "undefined") return;
    Object.keys(SOUND_DEFS).forEach(function (name) {
      if (sounds[name] === undefined) {
        sounds[name] = createHowl(name, SOUND_DEFS[name]);
      }
    });
  }

  function unlockAudio() {
    if (unlocked) return;
    unlocked = true;
    ensureSounds();
    // Howlerı kullanıcı jestiyle oynat
    try {
      if (
        typeof Howler !== "undefined" &&
        Howler.ctx &&
        Howler.ctx.state === "suspended"
      ) {
        Howler.ctx.resume();
      }
    } catch (e) {
      /* ignore */
    }
  }

  /**
   * Önceki sayfada playAfterRedirect ile kaydedilen sesi çal ve temizle.
   * @returns {boolean} pending ses çalındıysa true
   */
  function consumePendingSound() {
    var name = null;
    try {
      name = global.sessionStorage.getItem(PENDING_KEY);
      if (name) {
        global.sessionStorage.removeItem(PENDING_KEY);
      }
    } catch (e) {
      return false;
    }

    if (!name) return false;
    SoundManager.play(name);
    return true;
  }

  var SoundManager = {
    /**
     * Güvenli oynatma — tanımsız/yüklenemeyen seslerde hata fırlatmaz.
     * @param {string} name
     */
    play: function (name) {
      if (!name || typeof name !== "string") return;

      ensureSounds();
      var howl = sounds[name];
      if (!howl) return;

      try {
        // Aynı ses üst üste binmesin diye önce durdurup yeniden başlat
        howl.stop();
        howl.play();
      } catch (e) {
        // Sessizce yoksay
      }
    },

    /**
     * Full-page redirect / form POST öncesi çağır.
     * Ses bir sonraki sayfa yüklemesinde (DOMContentLoaded) çalınır.
     * @param {string} soundName
     */
    playAfterRedirect: function (soundName) {
      if (!soundName || typeof soundName !== "string") return;
      try {
        global.sessionStorage.setItem(PENDING_KEY, soundName);
      } catch (e) {
        // sessionStorage yoksa anında çalmayı dene (kesilebilir)
        SoundManager.play(soundName);
      }
    },

    /**
     * AJAX sonrası reload/navigate: önce sesi çal, sonra gecikmeli yönlendir.
     * @param {string} soundName
     * @param {function(): void} navigateFn
     * @param {number} [delayMs=600]
     */
    playThenNavigate: function (soundName, navigateFn, delayMs) {
      SoundManager.play(soundName);
      var delay = typeof delayMs === "number" ? delayMs : 600;
      if (typeof navigateFn === "function") {
        setTimeout(navigateFn, delay);
      }
    },

    /** Sesleri yeniden yükle (test / hot-reload) */
    reload: function () {
      Object.keys(sounds).forEach(function (k) {
        try {
          if (sounds[k]) sounds[k].unload();
        } catch (e) {
          /* ignore */
        }
      });
      sounds = Object.create(null);
      ensureSounds();
    },
  };

  function playPageFeedbackSounds() {
    // Kritik başarı (uzun efekt) — sipariş onayı vb.
    if (document.querySelector('[data-sound="success1"]')) {
      SoundManager.play("success1");
      return;
    }

    // TempData / Bootstrap alert'ler
    if (
      document.querySelector(
        '.alert-success, .sqm-alert-flash.ok, [data-sound="success"]',
      )
    ) {
      SoundManager.play("success");
      return;
    }
    if (
      document.querySelector(
        '.alert-danger, .sqm-alert-flash.err, [data-sound="error"]',
      )
    ) {
      SoundManager.play("error");
      return;
    }

    // ModelState validation-summary (geçerli olmayan)
    var summaries = document.querySelectorAll(
      ".validation-summary-errors, .sqm-auth-summary, .sqm-create-summary, .sqm-validation-summary",
    );
    for (var i = 0; i < summaries.length; i++) {
      var el = summaries[i];
      if (el.classList.contains("validation-summary-valid")) continue;
      var text = (el.textContent || "").replace(/\s+/g, " ").trim();
      if (text.length > 0) {
        SoundManager.play("error");
        return;
      }
    }

    // AccessDenied yönlendirmesi
    if (/\/Account\/AccessDenied/i.test(global.location.pathname)) {
      SoundManager.play("error");
    }
  }

  function wireSweetAlert() {
    if (typeof Swal === "undefined" || !Swal.fire) return;
    var originalFire = Swal.fire.bind(Swal);

    Swal.fire = function () {
      var args = arguments;
      var opts = args[0];
      var icon = null;

      if (opts && typeof opts === "object" && !Array.isArray(opts)) {
        icon = opts.icon;
      }

      if (icon === "success") {
        SoundManager.play("success");
      } else if (icon === "error" || icon === "warning") {
        SoundManager.play("error");
      }

      return originalFire.apply(Swal, args);
    };
  }

  function init() {
    ensureSounds();
    wireSweetAlert();

    // Önce redirect'ten ertelenen ses; yoksa sayfa flash / data-sound
    if (!consumePendingSound()) {
      playPageFeedbackSounds();
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }

  // İlk kullanıcı etkileşiminde audio context'i aç (autoplay politikası)
  ["pointerdown", "keydown", "touchstart"].forEach(function (evt) {
    document.addEventListener(evt, unlockAudio, { once: true, capture: true });
  });

  global.SoundManager = SoundManager;
})(typeof window !== "undefined" ? window : this);
