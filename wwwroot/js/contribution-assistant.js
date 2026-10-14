/**
 * contribution-assistant.js
 * Loaded after contribution-wizard.js on the "Make your data FAIR" form (Views/Submissions/Edit.cshtml).
 *
 * 1. Where answers came from
 *    Answers filled by the assistant (Accept) or by the Excel import are remembered with the value they got.
 *    They carry a small mark on the form and are posted with every save in the hidden "_sources" field, so
 *    the history (submission_change) records them as "assistant" / "import". As soon as the person changes
 *    such an answer it becomes their own ("user") and the mark disappears. Marks of earlier sessions come
 *    from the server (data-sources on the form).
 *
 * 2. The assistant panel (#wz-ai, only when the assistant is enabled)
 *    Chat with the server (POST .../Assistant), which answers with text and suggested answers.
 *    A suggestion is applied to the form only when the person clicks Accept; the wizard's autosave then
 *    stores it. The assistant itself never saves or submits anything.
 */
(function () {
    "use strict";

    var form = document.getElementById("wz-form");
    if (!form) return;

    var sourcesInput = document.getElementById("wz-sources");

    // ------------------------------------------------------------------ form values

    function inputOf(path) {
        var el = form.elements.namedItem(path);
        return el && el.length !== undefined && !el.tagName ? el[0] : el;   // RadioNodeList → first element
    }

    function valueOf(path) {
        var el = inputOf(path);
        return el ? String(el.value || "").trim() : "";
    }

    function allPaths() {
        return Array.prototype.map.call(form.querySelectorAll(".wz-field[data-path]"), function (w) { return w.dataset.path; });
    }

    function allValues() {
        var values = {};
        allPaths().forEach(function (p) {
            var v = valueOf(p);
            if (v) values[p] = v;
        });
        return values;
    }

    function wrapperOf(path) {
        return form.querySelector('.wz-field[data-path="' + cssEscape(path) + '"]');
    }

    function cssEscape(s) {
        return window.CSS && CSS.escape ? CSS.escape(s) : String(s).replace(/["\\]/g, "\\$&");
    }

    // ------------------------------------------------------------------ 1. sources and marks

    var LABELS = {
        assistant: { text: "Suggested by the assistant", icon: "bi-stars" },
        import: { text: "From your Excel file", icon: "bi-file-earmark-excel" }
    };

    var marks = {};   // path → { source, value }

    function writeSources() {
        if (!sourcesInput) return;
        var map = {};
        Object.keys(marks).forEach(function (p) { map[p] = marks[p].source; });
        sourcesInput.value = JSON.stringify(map);
    }

    function renderMark(path) {
        var w = wrapperOf(path);
        if (!w) return;
        var label = w.querySelector(".wz-label");
        var old = w.querySelector(".wz-src");
        if (old) old.remove();
        w.classList.remove("wz-src-assistant", "wz-src-import");

        var m = marks[path];
        if (!m || !label) return;
        var info = LABELS[m.source];
        if (!info) return;
        var badge = document.createElement("span");
        badge.className = "wz-src wz-src-badge-" + m.source;
        badge.title = "Please check this answer. It becomes yours as soon as you change it.";
        badge.innerHTML = '<i class="bi ' + info.icon + '"></i> ';
        badge.appendChild(document.createTextNode(info.text));
        label.appendChild(badge);
        w.classList.add("wz-src-" + m.source);
    }

    function setMark(path, source) {
        var v = valueOf(path);
        if (!v) { delete marks[path]; }
        else { marks[path] = { source: source, value: v }; }
        renderMark(path);
        writeSources();
    }

    // Any change: an answer that no longer has the value the assistant/import gave it is now the person's own
    function checkMarks() {
        var changed = false;
        Object.keys(marks).forEach(function (p) {
            if (valueOf(p) !== marks[p].value) {
                delete marks[p];
                renderMark(p);
                changed = true;
            }
        });
        if (changed) writeSources();
    }

    form.addEventListener("input", checkMarks);
    form.addEventListener("change", checkMarks);

    // Marks from earlier sessions
    try {
        var initial = JSON.parse(form.dataset.sources || "{}");
        Object.keys(initial).forEach(function (p) {
            if (LABELS[initial[p]] && valueOf(p)) marks[p] = { source: initial[p], value: valueOf(p) };
        });
    } catch (e) { /* no marks */ }
    Object.keys(marks).forEach(renderMark);
    writeSources();

    // Excel import: compare the answers before and after the file is read
    var beforeImport = null;
    var zone = document.getElementById("prefill-zone");
    var fileInput = document.getElementById("prefill-file-input");
    if (zone) zone.addEventListener("drop", function () { beforeImport = allValues(); }, true);
    if (fileInput) fileInput.addEventListener("change", function () { beforeImport = allValues(); }, true);

    document.addEventListener("pidar:prefilled", function (e) {
        if (e.detail && e.detail.source === "assistant") return;   // our own Accept, handled there
        if (!beforeImport) return;
        var after = allValues();
        Object.keys(after).forEach(function (p) {
            if (after[p] !== beforeImport[p]) marks[p] = { source: "import", value: after[p] };
        });
        beforeImport = null;
        allPaths().forEach(renderMark);
        writeSources();
    });

    // ------------------------------------------------------------------ 2. assistant panel

    var panel = document.getElementById("wz-ai");
    if (!panel) return;

    var log = document.getElementById("wz-ai-log");
    var chatForm = document.getElementById("wz-ai-form");
    var input = document.getElementById("wz-ai-input");
    var sendBtn = document.getElementById("wz-ai-send");
    var askUrl = panel.dataset.askUrl;
    var historyUrl = panel.dataset.historyUrl;
    var historyLoaded = false;
    var busy = false;

    function token() {
        var t = form.querySelector('input[name="__RequestVerificationToken"]');
        return t ? t.value : "";
    }

    function currentStep() {
        var s = document.getElementById("wz-step-input");
        return s ? parseInt(s.value, 10) || 0 : 0;
    }

    // Show the panel beside the form on wide screens instead of over it
    panel.addEventListener("shown.bs.offcanvas", function () {
        document.body.classList.add("wz-ai-open");
        if (!historyLoaded) loadHistory();
        input.focus();
    });
    panel.addEventListener("hidden.bs.offcanvas", function () { document.body.classList.remove("wz-ai-open"); });

    // ---------- rendering

    function escapeHtml(s) {
        return String(s == null ? "" : s)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    // Tiny, safe formatting for replies: **bold**, `code`, line breaks (text is escaped first)
    function format(text) {
        return escapeHtml(text)
            .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
            .replace(/`([^`]+)`/g, "<code>$1</code>")
            .replace(/\n/g, "<br>");
    }

    function scrollDown() { log.scrollTop = log.scrollHeight; }

    function addMessage(role, text, extraClass) {
        var div = document.createElement("div");
        div.className = "wz-ai-msg " + (role === "user" ? "wz-ai-user" : "wz-ai-bot") + (extraClass ? " " + extraClass : "");
        if (role === "user") div.textContent = text;
        else div.innerHTML = format(text);
        log.appendChild(div);
        scrollDown();
        return div;
    }

    function addTyping() {
        var div = document.createElement("div");
        div.className = "wz-ai-msg wz-ai-bot wz-ai-typing";
        div.innerHTML = '<span></span><span></span><span></span><span class="visually-hidden">The assistant is writing…</span>';
        log.appendChild(div);
        scrollDown();
        return div;
    }

    /** Suggestions with Accept / Dismiss (live = from this session; old ones are shown read-only). */
    function addProposals(proposals, live) {
        if (!proposals || proposals.length === 0) return;
        var box = document.createElement("div");
        box.className = "wz-ai-proposals" + (live ? "" : " wz-ai-old");

        var head = document.createElement("div");
        head.className = "wz-ai-proposals-head";
        head.innerHTML = live
            ? '<i class="bi bi-lightbulb"></i> Suggested answers <span class="text-muted">(check each one)</span>'
            : '<i class="bi bi-clock-history"></i> Suggested earlier';
        box.appendChild(head);

        var pending = [];

        proposals.forEach(function (p) {
            var card = document.createElement("div");
            card.className = "wz-ai-prop";
            card.innerHTML =
                '<div class="wz-ai-prop-where">' + escapeHtml(p.stepTitle) + "</div>" +
                '<div class="wz-ai-prop-label">' + escapeHtml(p.label) + "</div>" +
                '<div class="wz-ai-prop-value">' + escapeHtml(p.display || p.value) + "</div>" +
                (p.current ? '<div class="wz-ai-prop-current">Replaces: ' + escapeHtml(p.current) + "</div>" : "") +
                (p.reason ? '<div class="wz-ai-prop-reason">' + escapeHtml(p.reason) + "</div>" : "");

            if (live) {
                var actions = document.createElement("div");
                actions.className = "wz-ai-prop-actions";
                var ok = document.createElement("button");
                ok.type = "button";
                ok.className = "btn btn-sm btn-success";
                ok.innerHTML = '<i class="bi bi-check2"></i> Accept';
                var no = document.createElement("button");
                no.type = "button";
                no.className = "btn btn-sm btn-outline-secondary";
                no.textContent = "Dismiss";
                actions.appendChild(ok);
                actions.appendChild(no);
                card.appendChild(actions);

                var item = { p: p, card: card, done: false };
                pending.push(item);
                ok.addEventListener("click", function () { accept(item, true); refreshAll(); });
                no.addEventListener("click", function () { finish(item, "Dismissed"); refreshAll(); });
            }
            box.appendChild(card);
        });

        var all = null;
        function refreshAll() {
            if (!all) return;
            all.hidden = pending.filter(function (i) { return !i.done; }).length < 2;
        }

        if (live && proposals.length > 1) {
            all = document.createElement("button");
            all.type = "button";
            all.className = "btn btn-sm btn-outline-success w-100 mt-1";
            all.innerHTML = '<i class="bi bi-check2-all"></i> Accept all remaining';
            all.addEventListener("click", function () {
                var open = pending.filter(function (i) { return !i.done; });
                open.forEach(function (i) { accept(i, false); });
                if (open.length > 0) showField(open[0].p.path);
                refreshAll();
            });
            box.appendChild(all);
        }

        log.appendChild(box);
        scrollDown();
    }

    function finish(item, text, cls) {
        item.done = true;
        var actions = item.card.querySelector(".wz-ai-prop-actions");
        if (actions) actions.innerHTML = '<span class="small ' + (cls || "text-muted") + '">' + escapeHtml(text) + "</span>";
        item.card.classList.add("wz-ai-prop-done");
    }

    // ---------- applying a suggestion to the form

    function accept(item, navigate) {
        if (item.done) return;
        var ok = applyValue(item.p.path, item.p.value);
        if (!ok) { finish(item, "This question is not on the form.", "text-danger"); return; }
        finish(item, "Added to the form", "text-success");
        if (navigate) showField(item.p.path);
    }

    function applyValue(path, value) {
        var el = inputOf(path);
        if (!el) return false;

        if (el.tagName === "SELECT" && !Array.prototype.some.call(el.options, function (o) { return o.value === value; })) {
            var opt = document.createElement("option");
            opt.value = value;
            opt.textContent = value;
            el.appendChild(opt);
        }
        el.value = value;

        // The wizard listens for these: hidden inputs (buttons, tick boxes, modality) and selects on "change",
        // text fields on "input" (both bubble to the form → visibility, progress, autosave)
        var type = (el.tagName === "INPUT" && el.type !== "hidden") || el.tagName === "TEXTAREA" ? "input" : "change";
        el.dispatchEvent(new Event(type, { bubbles: true }));
        if (type === "input") el.dispatchEvent(new Event("change", { bubbles: true }));

        // Redraw buttons and tick boxes from the hidden values, as after an Excel import
        document.dispatchEvent(new CustomEvent("pidar:prefilled", { detail: { source: "assistant", filled: 1 } }));

        setMark(path, "assistant");
        return true;
    }

    /** Opens the field's step in the wizard and highlights the field (uses the wizard's [data-goto] navigation). */
    function showField(path) {
        var w = wrapperOf(path);
        var step = w && w.closest(".wz-step");
        if (!step) return;
        goTo(step.dataset.step, path);
    }

    function goTo(stepIndex, focusPath) {
        var a = document.createElement("a");
        a.href = "#";
        a.hidden = true;
        a.dataset.goto = String(stepIndex);
        if (focusPath) a.dataset.focus = focusPath;
        document.body.appendChild(a);
        a.click();
        a.remove();
    }

    // ---------- talking to the server

    function loadHistory() {
        historyLoaded = true;
        fetch(historyUrl, { headers: { "Accept": "application/json" }, credentials: "same-origin" })
            .then(function (r) { return r.ok ? r.json() : { messages: [] }; })
            .then(function (data) {
                var msgs = data.messages || [];
                if (msgs.length === 0) return;
                var sep = document.createElement("div");
                sep.className = "wz-ai-sep";
                sep.textContent = "Earlier conversation";
                log.appendChild(sep);
                msgs.forEach(function (m) {
                    addMessage(m.role, m.text);
                    if (m.role === "assistant") addProposals(m.proposals, false);
                });
                var now = document.createElement("div");
                now.className = "wz-ai-sep";
                now.textContent = "Now";
                log.appendChild(now);
                scrollDown();
            })
            .catch(function () { /* the panel still works without the old messages */ });
    }

    function send(text) {
        if (busy) return;
        busy = true;
        sendBtn.disabled = true;
        addMessage("user", text);
        var typing = addTyping();

        fetch(askUrl, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json",
                "RequestVerificationToken": token()
            },
            body: JSON.stringify({ message: text, step: currentStep(), answers: allValues() })
        }).then(function (r) {
            var isJson = (r.headers.get("content-type") || "").indexOf("application/json") !== -1;
            if (!isJson) {
                throw new Error(r.status === 200 || r.status === 400
                    ? "Your session may have ended. Save your work, log in again in a new tab, then reload this page."
                    : "The assistant is not available right now (" + r.status + ").");
            }
            return r.json().then(function (data) {
                if (!r.ok) throw new Error(data.error || "The assistant is not available right now.");
                return data;
            });
        }).then(function (data) {
            typing.remove();
            addMessage("assistant", data.reply || "");
            addProposals(data.proposals, true);
            if (data.navigateStep !== null && data.navigateStep !== undefined && data.navigateStep !== currentStep()) {
                goTo(data.navigateStep, null);
            }
        }).catch(function (err) {
            typing.remove();
            addMessage("assistant", err.message || "Something went wrong.", "wz-ai-error");
            // keep what they wrote so they can try again
            if (!input.value) input.value = text;
        }).then(function () {
            busy = false;
            sendBtn.disabled = false;
            input.focus();
        });
    }

    chatForm.addEventListener("submit", function (e) {
        e.preventDefault();
        var text = input.value.trim();
        if (!text || busy) return;
        input.value = "";
        send(text);
    });

    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            chatForm.requestSubmit ? chatForm.requestSubmit() : chatForm.dispatchEvent(new Event("submit", { cancelable: true }));
        }
    });
})();
