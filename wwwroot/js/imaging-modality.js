/**
 * imaging-modality.js
 * Create/Edit dataset: the Imaging Modality pick-list (12 groups, several can be ticked).
 * - Ticking/unticking writes the joined value ("PET/CT, MRI") into the hidden input that is posted.
 * - metadata-prefill.js writes the Excel text into the hidden input and fires "change":
 *   the text is read with the same rules as Helpers/ImagingModalityGroups.cs (keep them in sync)
 *   and the matching buttons are ticked; parts that fit no group are added as "not in list".
 */
(function () {
    "use strict";

    var ALL = ["CT", "PET", "PET/CT", "SPECT", "SPECT/CT", "MRI", "PET/MRI", "US", "OI", "OA", "EPRI", "MPI"];

    var SYNONYMS = [
        [/\bmagnetic\s+resonance(\s+imaging)?\b|\bnmr\b|\bmri\b|\bmr\b/gi, "MRI"],
        [/\bpositron\s+emission\s+tomography\b|\bpet\b/gi, "PET"],
        [/\bsingle[\s-]+photon\s+emission(\s+computed)?\s+tomography\b|\bspect\b/gi, "SPECT"],
        [/\b(micro[\s-]?)?(computed\s+tomography|ct)\b/gi, "CT"],
        [/\bultrasound\b|\bultrasonography\b|\bus\b/gi, "US"],
        [/\b(photo|opto)[\s-]?acoustic(s)?(\s+imaging)?\b|\boa\b|\bpai\b/gi, "OA"],
        [/\boptical\s+imaging\b|\bfluorescen\w*(\s+imaging)?\b|\bbioluminescen\w*(\s+imaging)?\b|\boi\b|\bfli\b|\bbli\b/gi, "OI"],
        [/\belectron\s+paramagnetic\s+resonance(\s+imaging)?\b|\bepr(i)?\b/gi, "EPRI"],
        [/\bmagnetic\s+particle\s+imaging\b|\bmpi\b/gi, "MPI"]
    ];

    function abbrevsOf(item) {
        var found = [];
        var rest = item.replace(/PETCT/gi, "PET CT").replace(/SPECTCT/gi, "SPECT CT");
        SYNONYMS.forEach(function (s) {
            s[0].lastIndex = 0;
            if (s[0].test(rest)) {
                found.push(s[1]);
                s[0].lastIndex = 0;
                rest = rest.replace(s[0], " ");
            }
        });
        return found;
    }

    function has(list, a, b) { return list.length === 2 && list.indexOf(a) !== -1 && list.indexOf(b) !== -1; }

    // "PET/CT, Magnetic Resonance Imaging, X-ray" → { groups: ["PET/CT","MRI"], other: ["X-ray"] }
    function parse(value) {
        var groups = [], other = [];
        String(value || "").split(/\s*(?:,|;|\+|\band\b)\s*/i).forEach(function (raw) {
            var item = raw.replace(/\s+/g, " ").trim().replace(/\.+$/, "");
            if (!item) return;
            var a = abbrevsOf(item);
            if (has(a, "PET", "CT")) groups.push("PET/CT");
            else if (has(a, "SPECT", "CT")) groups.push("SPECT/CT");
            else if (has(a, "PET", "MRI")) groups.push("PET/MRI");
            else if (a.length === 0) { if (other.indexOf(item) === -1) other.push(item); }
            else a.forEach(function (x) { groups.push(x); });
        });
        return { groups: groups, other: other };
    }

    function init() {
        var picker = document.getElementById("imaging-modality-picker");
        var hidden = document.getElementById("imaging-modality");
        if (!picker || !hidden) return;
        var row = picker.querySelector("[role=group]");
        var extraCount = 1000;

        function boxes() { return Array.prototype.slice.call(picker.querySelectorAll(".modality-opt")); }

        function writeHidden() {
            var ticked = boxes().filter(function (b) { return b.checked; });
            var groups = ALL.filter(function (g) { return ticked.some(function (b) { return b.value === g; }); });
            var other = ticked.filter(function (b) { return b.classList.contains("modality-extra"); })
                              .map(function (b) { return b.value; });
            hidden.value = groups.concat(other).join(", ");
        }

        function addExtra(text) {
            var id = "modality-opt-" + (extraCount++);
            var box = document.createElement("input");
            box.type = "checkbox";
            box.className = "btn-check modality-opt modality-extra";
            box.id = id;
            box.value = text;
            box.autocomplete = "off";
            box.checked = true;
            var label = document.createElement("label");
            label.className = "btn btn-sm btn-outline-secondary";
            label.htmlFor = id;
            label.title = "Not one of the 12 groups: untick to remove";
            label.textContent = text + " (not in list)";
            row.appendChild(box);
            row.appendChild(label);
        }

        // value set from outside (Excel prefill): tick what it contains
        function readHidden() {
            var p = parse(hidden.value);
            boxes().forEach(function (b) {
                if (b.classList.contains("modality-extra")) { b.nextElementSibling.remove(); b.remove(); }
                else b.checked = p.groups.indexOf(b.value) !== -1;
            });
            p.other.forEach(addExtra);
            writeHidden();
            if (hidden.classList.contains("prefill-filled") || hidden.style.backgroundColor) {
                row.style.backgroundColor = hidden.style.backgroundColor || "#d1e7dd";
                row.style.transition = "background-color 2s";
                row.style.borderRadius = ".375rem";
            }
        }

        picker.addEventListener("change", function (e) {
            if (e.target.classList && e.target.classList.contains("modality-opt")) writeHidden();
        });
        hidden.addEventListener("change", readHidden);
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
    else init();
})();
