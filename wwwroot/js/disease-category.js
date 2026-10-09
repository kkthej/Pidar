/**
 * disease-category.js
 * Create/Edit dataset: when a Disease Category is chosen in the dropdown, put its DOID code
 * (data-doid on the option) into the "Doid Disease Category" field.
 * - Categories without a code clear the field only if it still holds
 *   one of the list's own codes, so a code typed by hand is never wiped.
 * - Also used by metadata-prefill.js, which fires a "change" event after setting the value.
 */
(function () {
    "use strict";

    function init() {
        var select = document.getElementById("disease-category");
        if (!select) return;
        var target = document.querySelector('[name="' + select.dataset.doidTarget + '"]');
        if (!target) return;

        var listCodes = Array.prototype.map.call(select.options, function (o) { return o.dataset.doid || ""; })
            .filter(Boolean);

        select.addEventListener("change", function () {
            var opt = select.selectedOptions[0];
            var doid = opt ? (opt.dataset.doid || "") : "";
            if (doid) {
                target.value = doid;
            } else if (listCodes.indexOf(target.value.trim()) !== -1) {
                target.value = "";
            }
            target.dispatchEvent(new Event("input", { bubbles: true }));
        });

        // the code follows the dropdown; typing in it is still allowed
        target.setAttribute("placeholder", "filled from Disease Category");
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
    else init();
})();
