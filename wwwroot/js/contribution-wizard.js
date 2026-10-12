/**
 * contribution-wizard.js
 * The "Make your data FAIR" form (Views/Submissions/Edit.cshtml).
 *
 *  - Steps: one <section class="wz-step"> per section of the metadata template, shown one at a time.
 *  - Show/hide: data-show-if="Field.Path|value1|value2" on a question (.wz-field) or a group
 *    (fieldset.wz-group). Same rule as ContributionForm.IsVisible on the server: the condition
 *    holds AND the question it looks at is itself visible.
 *  - Buttons / tick boxes: each YesNo / Choice / Multi question posts one hidden input; the buttons
 *    write into it, and it redraws the buttons on "change" (the Excel import fires that event).
 *  - Progress: required + visible + empty questions are counted per step (side bar, review step).
 *  - Autosave: 2 s after the last change, when changing step, and when leaving the page.
 *    Answers are stored as a draft on the server; nothing is published until a curator approves it.
 */
(function () {
    "use strict";

    var form = document.getElementById("wz-form");
    if (!form) return;

    var steps = Array.prototype.slice.call(form.querySelectorAll(".wz-step"));
    var stepCount = parseInt(form.dataset.stepCount, 10);     // form steps; index stepCount = review
    var stepInput = document.getElementById("wz-step-input");
    var statusEl = document.getElementById("wz-save-status");
    var current = parseInt(form.dataset.startStep, 10) || 0;

    var wrappers = Array.prototype.slice.call(form.querySelectorAll(".wz-field"));
    var wrapperByPath = {};
    wrappers.forEach(function (w) { wrapperByPath[w.dataset.path] = w; });

    // ------------------------------------------------------------------ values

    function inputOf(path) {
        var el = form.elements.namedItem(path);
        return el && el.length !== undefined && !el.tagName ? el[0] : el;   // RadioNodeList → first element
    }

    function valueOf(path) {
        var el = inputOf(path);
        return el ? String(el.value || "").trim() : "";
    }

    function norm(s) { return String(s || "").trim().toLowerCase(); }

    // ------------------------------------------------------------------ visibility

    function parseCondition(attr) {
        if (!attr) return null;
        var parts = attr.split("|");
        return { field: parts[0], values: parts.slice(1).map(norm) };
    }

    function holds(cond, depth) {
        if (!cond) return true;
        var controller = wrapperByPath[cond.field];
        if (controller && !isVisible(controller, depth + 1)) return false;
        return cond.values.indexOf(norm(valueOf(cond.field))) !== -1;
    }

    function isVisible(wrapper, depth) {
        depth = depth || 0;
        if (depth > 10) return true;
        var group = wrapper.closest(".wz-group");
        if (group && !holds(parseCondition(group.dataset.showIf), depth)) return false;
        return holds(parseCondition(wrapper.dataset.showIf), depth);
    }

    function applyVisibility() {
        wrappers.forEach(function (w) { w.hidden = !isVisible(w); });
        Array.prototype.forEach.call(form.querySelectorAll("fieldset.wz-group[data-show-if]"), function (g) {
            g.hidden = !holds(parseCondition(g.dataset.showIf), 0);
        });
    }

    // ------------------------------------------------------------------ choice buttons (YesNo / Choice)

    function initChoice(box) {
        var hidden = document.getElementById(box.dataset.for);
        var buttons = Array.prototype.slice.call(box.querySelectorAll(".wz-opt-btn"));
        var otherBtn = box.querySelector(".wz-other-btn");
        var otherText = box.querySelector(".wz-other-text");

        function draw() {
            var v = norm(hidden.value);
            var matched = false;
            buttons.forEach(function (b) {
                if (b.dataset.other) return;
                var on = v !== "" && norm(b.dataset.value) === v;
                matched = matched || on;
                setOn(b, on);
            });
            Array.prototype.forEach.call(box.querySelectorAll(".wz-extra"), function (x) { x.remove(); });
            if (otherBtn) {
                var otherOn = v !== "" && !matched;
                setOn(otherBtn, otherOn || otherBtn.dataset.open === "1");
                otherText.hidden = !(otherOn || otherBtn.dataset.open === "1");
                if (otherOn && document.activeElement !== otherText) otherText.value = hidden.value;
            } else if (v !== "" && !matched) {
                // a value from an Excel file that is not one of the buttons: keep it visible
                var extra = document.createElement("span");
                extra.className = "badge text-bg-light border wz-extra";
                extra.textContent = hidden.value + " (not in list)";
                box.appendChild(extra);
            }
        }

        function setOn(b, on) {
            b.classList.toggle("active", on);
            b.setAttribute("aria-checked", on ? "true" : "false");
        }

        function set(v) {
            hidden.value = v;
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
        }

        buttons.forEach(function (b) {
            b.addEventListener("click", function () {
                if (b.dataset.other) {
                    b.dataset.open = "1";
                    draw();
                    otherText.hidden = false;
                    otherText.focus();
                    if (buttons.some(function (x) { return !x.dataset.other && x.classList.contains("active"); })) set("");
                    return;
                }
                if (otherBtn) otherBtn.dataset.open = "";
                // clicking the selected button again clears the answer
                set(b.classList.contains("active") ? "" : b.dataset.value);
            });
        });

        if (otherText) {
            otherText.addEventListener("input", function () { set(otherText.value.trim()); });
        }

        hidden.addEventListener("change", draw);
        box._draw = draw;
        draw();
    }

    // ------------------------------------------------------------------ tick boxes (Multi)

    function initMulti(box) {
        var hidden = document.getElementById(box.dataset.for);
        var checks = Array.prototype.slice.call(box.querySelectorAll("input[type=checkbox]"));
        var otherText = box.querySelector(".wz-other-text");

        function split(v) {
            return String(v || "").split(/\s*[,;]\s*/).map(function (x) { return x.trim(); }).filter(Boolean);
        }

        function write() {
            var vals = checks.filter(function (c) { return c.checked; }).map(function (c) { return c.value; });
            if (otherText) vals = vals.concat(split(otherText.value));
            hidden.value = vals.join(", ");
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
        }

        function draw() {
            var items = split(hidden.value);
            var rest = items.slice();
            checks.forEach(function (c) {
                if (c.classList.contains("wz-extra-check")) return;
                var hit = items.filter(function (i) { return norm(i) === norm(c.value); });
                c.checked = hit.length > 0;
                rest = rest.filter(function (r) { return norm(r) !== norm(c.value); });
            });
            if (otherText) {
                if (document.activeElement !== otherText) otherText.value = rest.join(", ");
            } else {
                // values not in the list (e.g. from the Excel file) are kept as extra ticked boxes
                checks.filter(function (c) { return c.classList.contains("wz-extra-check"); }).forEach(function (c) {
                    c.closest(".form-check").remove();
                });
                checks = checks.filter(function (c) { return !c.classList.contains("wz-extra-check"); });
                rest.forEach(function (r) {
                    var wrap = document.createElement("div");
                    wrap.className = "form-check wz-check";
                    var c = document.createElement("input");
                    c.type = "checkbox";
                    c.className = "form-check-input wz-extra-check";
                    c.checked = true;
                    c.value = r;
                    c.id = box.dataset.for + "-x" + Math.random().toString(36).slice(2, 8);
                    var l = document.createElement("label");
                    l.className = "form-check-label";
                    l.htmlFor = c.id;
                    l.textContent = r + " (not in list)";
                    wrap.appendChild(c);
                    wrap.appendChild(l);
                    box.appendChild(wrap);
                    c.addEventListener("change", write);
                    checks.push(c);
                });
            }
        }

        checks.forEach(function (c) { c.addEventListener("change", write); });
        if (otherText) otherText.addEventListener("input", write);
        hidden.addEventListener("change", function (e) { if (e.isTrusted !== false) draw(); });
        box._draw = draw;
        draw();
    }

    Array.prototype.forEach.call(form.querySelectorAll(".wz-choice"), initChoice);
    Array.prototype.forEach.call(form.querySelectorAll(".wz-multi"), initMulti);

    function redrawAll() {
        Array.prototype.forEach.call(form.querySelectorAll(".wz-choice, .wz-multi"), function (b) { if (b._draw) b._draw(); });
        var modality = document.getElementById("imaging-modality");
        if (modality) modality.dispatchEvent(new Event("change", { bubbles: false }));
    }

    // "N/A" quick answer for required text questions that do not apply
    form.addEventListener("click", function (e) {
        var na = e.target.closest && e.target.closest(".wz-na");
        if (!na) return;
        var input = document.getElementById(na.dataset.naFor);
        if (!input) return;
        input.value = "N/A";
        input.dispatchEvent(new Event("input", { bubbles: true }));
    });

    // ------------------------------------------------------------------ checks shown while typing

    var FORMAT = {
        Email: { re: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, msg: "Please enter a valid email address." },
        Orcid: { re: /^(https?:\/\/orcid\.org\/)?\d{4}-\d{4}-\d{4}-\d{3}[\dX]$/i, msg: "An ORCID iD looks like 0000-0002-1825-0097." },
        Year: { re: /^(19|20)\d{2}$/, msg: "Please enter a four-digit year." },
        Number: { re: /^\d+$/, msg: "Please enter a whole number." },
        Url: { re: /^https?:\/\/\S+$/i, msg: "Please enter a full link starting with https://" }
    };

    function formatError(wrapper) {
        var rule = FORMAT[wrapper.dataset.kind];
        var v = valueOf(wrapper.dataset.path);
        if (!rule || !v || v.toUpperCase() === "N/A") return "";
        return rule.re.test(v) ? "" : rule.msg;
    }

    function showFormatError(wrapper) {
        var slot = wrapper.querySelector(".wz-error");
        if (slot) slot.textContent = formatError(wrapper);
    }

    // ------------------------------------------------------------------ progress

    function missingInStep(i) {
        var list = [];
        if (!steps[i]) return list;
        Array.prototype.forEach.call(steps[i].querySelectorAll(".wz-field"), function (w) {
            if (w.hidden || !isVisible(w)) return;
            var empty = w.dataset.required && !valueOf(w.dataset.path);
            var bad = formatError(w);
            if (empty || bad) {
                var label = w.querySelector(".wz-label");
                list.push({
                    path: w.dataset.path,
                    label: label ? label.childNodes[0].textContent.trim() : w.dataset.path,
                    message: empty ? "required" : bad
                });
            }
        });
        return list;
    }

    function requiredInStep(i) {
        var n = 0;
        Array.prototype.forEach.call(steps[i].querySelectorAll(".wz-field[data-required]"), function (w) {
            if (!w.hidden && isVisible(w)) n++;
        });
        return n;
    }

    function updateProgress() {
        var totalReq = 0, totalMissing = 0;
        for (var i = 0; i < stepCount; i++) {
            var missing = missingInStep(i);
            var req = requiredInStep(i);
            var missingRequired = missing.filter(function (m) { return m.message === "required"; }).length;
            totalReq += req;
            totalMissing += missingRequired;

            var badge = document.querySelector('[data-badge="' + i + '"]');
            if (badge) {
                badge.textContent = missing.length === 0 ? "✓" : (req - missingRequired) + "/" + req;
                badge.classList.toggle("done", missing.length === 0);
            }

            var state = document.querySelector('[data-review-state="' + i + '"]');
            var ul = document.querySelector('[data-review-missing="' + i + '"]');
            if (state) {
                state.innerHTML = missing.length === 0
                    ? '<i class="bi bi-check-circle-fill text-success"></i>'
                    : '<i class="bi bi-exclamation-circle-fill text-warning"></i>';
            }
            if (ul) {
                ul.innerHTML = "";
                missing.forEach(function (m) {
                    var li = document.createElement("li");
                    var a = document.createElement("a");
                    a.href = "#";
                    a.dataset.goto = String(i);
                    a.dataset.focus = m.path;
                    a.textContent = m.label;
                    li.appendChild(a);
                    li.appendChild(document.createTextNode(m.message === "required" ? " — missing" : " — " + m.message));
                    ul.appendChild(li);
                });
            }
        }

        var done = totalReq - totalMissing;
        var pct = totalReq === 0 ? 100 : Math.round(100 * done / totalReq);
        var bar = document.getElementById("wz-overall-bar");
        var txt = document.getElementById("wz-overall-text");
        if (bar) bar.style.width = pct + "%";
        if (txt) txt.textContent = done + " / " + totalReq;

        var blocked = document.getElementById("wz-submit-blocked");
        if (blocked) {
            blocked.hidden = totalMissing === 0;
            blocked.textContent = totalMissing === 0 ? "" :
                totalMissing + (totalMissing === 1 ? " required answer is" : " required answers are") +
                " still missing (see the list above). You can save now and finish later.";
        }
    }

    // ------------------------------------------------------------------ steps

    function goTo(i, focusPath) {
        i = Math.max(0, Math.min(stepCount, i));
        if (dirty) save();
        current = i;
        steps.forEach(function (s) { s.hidden = parseInt(s.dataset.step, 10) !== i; });
        Array.prototype.forEach.call(document.querySelectorAll(".wz-nav-item"), function (b) {
            b.classList.toggle("active", parseInt(b.dataset.goto, 10) === i);
        });
        if (stepInput) stepInput.value = String(i);
        updateProgress();

        try {
            var url = new URL(window.location.href);
            url.searchParams.set("step", String(i));
            url.searchParams.delete("check");
            window.history.replaceState(null, "", url.toString());
        } catch (e) { /* old browser: ignore */ }

        var target = focusPath && wrapperByPath[focusPath];
        if (target) {
            target.scrollIntoView({ behavior: "smooth", block: "center" });
            var el = target.querySelector("input:not([type=hidden]), textarea, select, button");
            if (el) setTimeout(function () { el.focus({ preventScroll: true }); }, 300);
            target.classList.add("wz-flash");
            setTimeout(function () { target.classList.remove("wz-flash"); }, 1600);
        } else {
            var top = form.getBoundingClientRect().top + window.scrollY - 120;
            window.scrollTo({ top: Math.max(0, top), behavior: "smooth" });
        }
    }

    document.addEventListener("click", function (e) {
        var t = e.target.closest && e.target.closest("[data-goto]");
        if (!t) return;
        e.preventDefault();
        goTo(parseInt(t.dataset.goto, 10), t.dataset.focus);
    });

    // ------------------------------------------------------------------ autosave

    var dirty = false, timer = null, saving = false, again = false;

    function setStatus(text, cls) {
        if (!statusEl) return;
        statusEl.textContent = text;
        statusEl.className = "wz-save-status small " + (cls || "text-muted");
    }

    function markDirty() {
        dirty = true;
        setStatus("Unsaved changes…");
        clearTimeout(timer);
        timer = setTimeout(save, 2000);
    }

    function save() {
        clearTimeout(timer);
        if (saving) { again = true; return; }
        saving = true;
        dirty = false;
        setStatus("Saving…");
        fetch(form.action, {
            method: "POST",
            body: new FormData(form),
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        }).then(function (r) {
            var json = (r.headers.get("content-type") || "").indexOf("application/json") !== -1;
            if (!r.ok || !json) throw new Error(r.status === 200 ? "session" : String(r.status));
            return r.json();
        }).then(function (data) {
            var t = new Date(data.savedAt);
            setStatus("All changes saved · " + t.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }), "text-success");
        }).catch(function (err) {
            dirty = true;
            setStatus(err.message === "session"
                ? "Not saved: your session has ended. Copy any long text, then log in again in a new tab."
                : "Not saved (connection problem). Retrying…", "text-danger");
            if (err.message !== "session") { clearTimeout(timer); timer = setTimeout(save, 10000); }
        }).then(function () {
            saving = false;
            if (again) { again = false; save(); }
        });
    }

    function onEdit(e) {
        var w = e.target.closest && e.target.closest(".wz-field");
        applyVisibility();
        if (w) showFormatError(w);
        updateProgress();
        markDirty();
    }

    form.addEventListener("input", onEdit);
    form.addEventListener("change", onEdit);

    // Leaving the page with unsaved answers: send them in the background
    window.addEventListener("pagehide", function () {
        if (dirty && navigator.sendBeacon) navigator.sendBeacon(form.action, new FormData(form));
    });

    // Normal submits (Save and finish later / Submit) save everything themselves
    form.addEventListener("submit", function () { dirty = false; clearTimeout(timer); });

    // After the Excel import fills the fields: redraw buttons, re-check, save
    document.addEventListener("pidar:prefilled", function () {
        redrawAll();
        applyVisibility();
        wrappers.forEach(showFormatError);
        updateProgress();
        markDirty();
    });

    // ------------------------------------------------------------------ start

    applyVisibility();
    updateProgress();
    goTo(current, null);
    // goTo scrolls; on first load stay at the top of the page
    window.scrollTo(0, 0);
})();
