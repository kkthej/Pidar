/**
 * idle-logout.js — signs the user out after a period without activity (only loaded when signed in).
 *
 * - Activity = mouse, keyboard, scroll, touch. Shared across tabs (localStorage), so working in one
 *   tab keeps the others signed in, and signing out in one signs out all.
 * - A countdown (h:mm:ss) is shown in the session bar above the navbar (Views/Shared/_SessionBar.cshtml),
 *   whose "renew" link restarts it; 2 minutes before the end a warning appears with
 *   "Stay signed in" / "Sign out now".
 * - While the user is active, the server is pinged every few minutes so the sign-in cookie stays valid
 *   even if they only read or scroll. The server also ends idle sessions on its own (Program.cs).
 * - Coming back to a tab after the time is up (e.g. laptop asleep) signs out immediately.
 */
(function () {
    "use strict";

    var script = document.currentScript;
    var IDLE_MS = (parseFloat(script && script.dataset.idleMinutes) || 15) * 60 * 1000;
    var WARN_MS = (parseFloat(script && script.dataset.warnSeconds) || 120) * 1000;
    var KEEPALIVE_URL = script && script.dataset.keepaliveUrl;
    var LOGIN_URL = (script && script.dataset.loginUrl) || "/Identity/Account/Login?timedOut=1";
    var PING_EVERY_MS = 4 * 60 * 1000;
    var KEY_ACTIVITY = "pidar.lastActivity";
    var KEY_SIGNEDOUT = "pidar.signedOutAt";

    // ---- shared "last activity" time (falls back to memory if storage is blocked) ----
    var memoryLast = Date.now();
    function readLast() {
        try { var v = parseInt(localStorage.getItem(KEY_ACTIVITY), 10); if (v) return Math.max(v, memoryLast); } catch (e) { }
        return memoryLast;
    }
    function writeLast(t) {
        memoryLast = t;
        try { localStorage.setItem(KEY_ACTIVITY, String(t)); } catch (e) { }
    }

    // A fresh sign-in starts a fresh countdown: a stale time left from a previous session must not log out at once
    try {
        var stale = parseInt(localStorage.getItem(KEY_ACTIVITY), 10);
        var signedOut = parseInt(localStorage.getItem(KEY_SIGNEDOUT), 10);
        if (!stale || (signedOut && signedOut >= stale) || document.referrer.indexOf("/Account/Login") !== -1) {
            writeLast(Date.now());
        }
    } catch (e) { writeLast(Date.now()); }

    var lastPing = 0, activitySincePing = false, warningShown = false, signingOut = false;

    // ---- UI: countdown (session bar, and the older menu badge if a page still has it) + warning panel ----
    var badge = document.getElementById("session-timer");
    var barTimer = document.getElementById("session-bar-timer");

    // "Last login" in the session bar: show it in the viewer's local time (the server renders UTC)
    var lastLoginEl = document.getElementById("session-bar-last-login");
    if (lastLoginEl && lastLoginEl.getAttribute("datetime")) {
        var d = new Date(lastLoginEl.getAttribute("datetime"));
        if (!isNaN(d)) {
            var p2 = function (n) { return String(n).padStart(2, "0"); };
            lastLoginEl.textContent = d.getFullYear() + "-" + p2(d.getMonth() + 1) + "-" + p2(d.getDate()) + " " +
                p2(d.getHours()) + ":" + p2(d.getMinutes()) + ":" + p2(d.getSeconds());
        }
    }
    var panel = document.createElement("div");
    panel.className = "idle-warning";
    panel.setAttribute("role", "alertdialog");
    panel.setAttribute("aria-live", "assertive");
    panel.setAttribute("aria-labelledby", "idle-warning-title");
    panel.hidden = true;
    panel.innerHTML =
        '<div class="idle-warning-title" id="idle-warning-title">Still there?</div>' +
        '<div class="idle-warning-text">You will be signed out in <strong class="idle-warning-time">2:00</strong> because of inactivity.</div>' +
        '<div class="idle-warning-actions">' +
        '<button type="button" class="btn btn-primary btn-sm" data-idle="stay">Stay signed in</button>' +
        '<button type="button" class="btn btn-outline-secondary btn-sm" data-idle="logout">Sign out now</button>' +
        '</div>';
    document.body.appendChild(panel);
    var panelTime = panel.querySelector(".idle-warning-time");

    var style = document.createElement("style");
    style.textContent =
        ".session-timer{display:inline-block;margin-left:.4rem;padding:.05rem .45rem;border-radius:999px;font-size:.75rem;" +
        "font-variant-numeric:tabular-nums;background:#eef1f4;color:#52514e;vertical-align:1px}" +
        ".session-timer.warn{background:#fff3cd;color:#8a5a00}" +
        ".idle-warning{position:fixed;right:1rem;bottom:1rem;z-index:2000;width:min(340px,calc(100vw - 2rem));background:#fff;" +
        "border:1px solid #e3e3e0;border-left:4px solid #eda100;border-radius:10px;box-shadow:0 10px 30px rgba(0,0,0,.15);padding:1rem 1.1rem}" +
        ".idle-warning-title{font-weight:600;margin-bottom:.25rem}.idle-warning-text{font-size:.9rem;color:#333;margin-bottom:.75rem}" +
        ".idle-warning-time{font-variant-numeric:tabular-nums}.idle-warning-actions{display:flex;gap:.5rem;flex-wrap:wrap}";
    document.head.appendChild(style);

    function fmt(ms) {
        var s = Math.max(0, Math.ceil(ms / 1000));
        return Math.floor(s / 60) + ":" + String(s % 60).padStart(2, "0");
    }
    // XNAT style, e.g. 0:14:58
    function fmtLong(ms) {
        var s = Math.max(0, Math.ceil(ms / 1000));
        return Math.floor(s / 3600) + ":" + String(Math.floor(s % 3600 / 60)).padStart(2, "0") + ":" + String(s % 60).padStart(2, "0");
    }

    // ---- activity ----
    var lastWrite = 0;
    function onActivity() {
        if (signingOut || warningShown) return;     // while warned, only the buttons (or a key/click) count
        var now = Date.now();
        activitySincePing = true;
        if (now - lastWrite > 1000) { lastWrite = now; writeLast(now); }
    }
    ["mousemove", "mousedown", "wheel", "scroll", "touchstart"].forEach(function (ev) {
        window.addEventListener(ev, onActivity, { passive: true, capture: true });
    });
    ["keydown", "click"].forEach(function (ev) {
        window.addEventListener(ev, function (e) {
            if (warningShown && panel.contains(e.target)) return;   // handled by the buttons
            if (warningShown) stay(); else onActivity();
        }, true);
    });

    function ping() {
        if (!KEEPALIVE_URL) return;
        lastPing = Date.now();
        activitySincePing = false;
        fetch(KEEPALIVE_URL, { credentials: "same-origin", cache: "no-store", redirect: "manual" })
            .then(function (r) {
                // signed out on the server (expired, or signed out elsewhere): go to the login page
                if (r.type === "opaqueredirect" || r.status === 401 || r.status === 403) goToLogin();
            })
            .catch(function () { /* offline: try again later */ });
    }

    function stay() {
        writeLast(Date.now());
        hideWarning();
        ping();
        tick();
    }

    // "renew" in the session bar: restart the countdown and refresh the server cookie straight away
    document.addEventListener("click", function (e) {
        var link = e.target.closest && e.target.closest("[data-idle-renew]");
        if (!link) return;
        e.preventDefault();
        stay();
    });

    function showWarning() {
        if (warningShown) return;
        warningShown = true;
        panel.hidden = false;
        var btn = panel.querySelector('[data-idle="stay"]');
        try { btn.focus({ preventScroll: true }); } catch (e) { btn.focus(); }
    }
    function hideWarning() {
        warningShown = false;
        panel.hidden = true;
    }

    panel.addEventListener("click", function (e) {
        var action = e.target.getAttribute && e.target.getAttribute("data-idle");
        if (action === "stay") stay();
        if (action === "logout") signOut();
    });

    function goToLogin() {
        if (signingOut) return;
        signingOut = true;
        window.location.href = LOGIN_URL;
    }

    function signOut() {
        if (signingOut) return;
        signingOut = true;
        try { localStorage.setItem(KEY_SIGNEDOUT, String(Date.now())); } catch (e) { }
        var form = document.getElementById("idle-logout-form");
        if (form) form.submit(); else window.location.href = LOGIN_URL;
    }

    // another tab signed out: follow it
    window.addEventListener("storage", function (e) {
        if (e.key === KEY_SIGNEDOUT && e.newValue) goToLogin();
        if (e.key === KEY_ACTIVITY && warningShown && readLast() + IDLE_MS - Date.now() > WARN_MS) hideWarning();
    });

    // ---- the clock ----
    function tick() {
        if (signingOut) return;
        var left = readLast() + IDLE_MS - Date.now();

        if (left <= 0) { signOut(); return; }

        if (badge) {
            badge.hidden = false;
            badge.textContent = fmt(left);
            badge.classList.toggle("warn", left <= WARN_MS);
            badge.setAttribute("aria-label", "Signed out in " + fmt(left) + " without activity");
        }
        if (barTimer) {
            barTimer.textContent = fmtLong(left);
            barTimer.classList.toggle("warn", left <= WARN_MS);
        }
        if (left <= WARN_MS) { showWarning(); panelTime.textContent = fmt(left); }
        else if (warningShown) hideWarning();     // activity in another tab

        if (activitySincePing && Date.now() - lastPing > PING_EVERY_MS) ping();
    }

    tick();
    setInterval(tick, 1000);
    // returning to the tab (e.g. after sleep): check straight away
    document.addEventListener("visibilitychange", function () { if (!document.hidden) tick(); });
})();
