document.addEventListener('DOMContentLoaded', function () {

    // ============================================================
    // Helpers
    // ============================================================
    function triggerBrowserDownload(url) {
        // Single, native download — server sets filename
        window.location.href = `${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}`;
    }

    function syncSelectAllState() {
        if (!selectAllDatasets) return;

        const total = datasetItems.length;
        const selected = selectedIds.size;

        selectAllDatasets.checked = total > 0 && selected === total;
        selectAllDatasets.indeterminate = selected > 0 && selected < total;
    }


    // ============================================================
    // Elements (ALL download)
    // ============================================================
    const downloadTypeDropdown = document.getElementById('downloadType');
    const downloadButton = document.getElementById('downloadButton');

    // ============================================================
    // Enable/disable Download ALL button
    // ============================================================
    if (downloadTypeDropdown && downloadButton) {
        downloadTypeDropdown.addEventListener('change', () => {
            downloadButton.disabled = !downloadTypeDropdown.value;
        });
        downloadButton.disabled = !downloadTypeDropdown.value;
    }

    // ============================================================
    // Download ALL datasets (browser-native, single trigger)
    // ============================================================
    if (downloadButton && downloadTypeDropdown && !downloadButton.dataset.bound) {
        downloadButton.dataset.bound = "1";
        let busyAll = false;

        downloadButton.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (busyAll) return;

            const format = downloadTypeDropdown.value;
            if (!format) return;

            const url = getDownloadUrl(format);
            if (!url) return;

            busyAll = true;
            downloadButton.disabled = true;
            downloadButton.textContent = "Preparing…";

            triggerBrowserDownload(url);

            setTimeout(() => {
                busyAll = false;
                downloadButton.disabled = !downloadTypeDropdown.value;
                downloadButton.textContent = "Download";
            }, 1500);
        });
    }

    function getDownloadUrl(format) {
        const map = {
            csv: '/Download/DownloadCsv',
            pdf: '/Download/DownloadPdf',
            json: '/Download/DownloadJson',
            xlsx: '/Download/DownloadXlsx'
        };
        return map[(format || "").toLowerCase()] || "";
    }

    // ============================================================
    // Template & Guidelines download (unchanged, still fetch+blob)
    // ============================================================
    const templateDownloadBtn = document.getElementById('download-template');
    const guidelinesDownloadBtn = document.getElementById('download-guidelines');

    async function fetchAndDownload(url, filename) {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`Failed to download ${filename}`);
        const blob = await res.blob();

        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    if (templateDownloadBtn && !templateDownloadBtn.dataset.bound) {
        templateDownloadBtn.dataset.bound = "1";
        templateDownloadBtn.addEventListener('click', (e) => {
            e.preventDefault();
            fetchAndDownload("/contribution_files/metadata_template.xlsx", "metadata_template.xlsx");
        });
    }

    if (guidelinesDownloadBtn && !guidelinesDownloadBtn.dataset.bound) {
        guidelinesDownloadBtn.dataset.bound = "1";
        guidelinesDownloadBtn.addEventListener('click', (e) => {
            e.preventDefault();
            fetchAndDownload("/contribution_files/guidelines.docx", "guidelines.docx");
        });
    }

    // ============================================================
    // Download SELECTED datasets (shared mechanism)
    // ============================================================
    const datasetCheckboxList = document.getElementById('datasetCheckboxList');
    const selectAllDatasets = document.getElementById('selectAllDatasets');
    const selectedDownloadType = document.getElementById('selectedDownloadType');
    const downloadSelectedButton = document.getElementById('downloadSelectedButton');
    const selectedCountBadge = document.getElementById('selectedCountBadge');
    const selectedCountText = document.getElementById('selectedCountText');
    const selectedProgressText = document.getElementById('selectedProgressText');

    let datasetItems = [];
    let selectedIds = new Set();

    function setSelectedUi() {
        const count = selectedIds.size;
        if (selectedCountBadge) selectedCountBadge.textContent = String(count);
        if (selectedCountText) selectedCountText.textContent = String(count);

        const ok = count > 0 && selectedDownloadType?.value;
        if (downloadSelectedButton) downloadSelectedButton.disabled = !ok;
    }

    function buildLabel(d) {
        const bits = [];
        if (d.species) bits.push(d.species);
        if (d.modality) bits.push(d.modality);
        if (d.title) bits.push(d.title);
        return `Dataset ${d.displayId}${bits.length ? " — " + bits.join(" | ") : ""}`;
    }

    async function loadDatasetsForDropdown() {
        if (!datasetCheckboxList) return;

        try {
            const res = await fetch(`/Download/DatasetList?t=${Date.now()}`);
            if (!res.ok) throw new Error();

            const list = await res.json();
            datasetItems = list.map(d => ({
                displayId: d.displayId,
                label: buildLabel(d)
            }));

            renderDatasetList();
            syncSelectAllState();
            setSelectedUi();
        } catch {
            datasetCheckboxList.innerHTML =
                `<div class="text-danger small">Failed to load datasets.</div>`;
        }
    }

    function renderDatasetList() {
        if (!datasetCheckboxList) return;

        datasetCheckboxList.innerHTML = datasetItems.map(d => {
            const checked = selectedIds.has(d.displayId) ? "checked" : "";
            return `
            <div class="form-check">
                <input class="form-check-input dataset-cb"
                       type="checkbox"
                       data-id="${d.displayId}"
                       id="ds_${d.displayId}"
                       ${checked}>
                <label class="form-check-label" for="ds_${d.displayId}">
                    ${d.label}
                </label>
            </div>
        `;
        }).join("");
    }

    if (datasetCheckboxList && !datasetCheckboxList.dataset.bound) {
        datasetCheckboxList.dataset.bound = "1";

        datasetCheckboxList.addEventListener('change', (e) => {
            const cb = e.target;
            if (!cb.classList.contains('dataset-cb')) return;

            const id = parseInt(cb.dataset.id, 10);
            if (cb.checked) selectedIds.add(id);
            else selectedIds.delete(id);

            syncSelectAllState();

            setSelectedUi();
        });
    }

    if (selectAllDatasets && !selectAllDatasets.dataset.bound) {
        selectAllDatasets.dataset.bound = "1";

        selectAllDatasets.addEventListener('change', () => {
            if (selectAllDatasets.checked) {
                selectedIds = new Set(datasetItems.map(d => d.displayId));
            } else {
                selectedIds.clear();
            }
            renderDatasetList();
            syncSelectAllState();
            setSelectedUi();
        });
    }

    if (selectedDownloadType && !selectedDownloadType.dataset.bound) {
        selectedDownloadType.dataset.bound = "1";
        selectedDownloadType.addEventListener('change', setSelectedUi);
    }

    function getSelectedDownloadUrl(format, idsCsv) {
        const map = {
            csv: '/Download/DownloadSelectedCsv',
            pdf: '/Download/DownloadSelectedPdf',
            json: '/Download/DownloadSelectedJson',
            xlsx: '/Download/DownloadSelectedXlsx'
        };
        const base = map[(format || "").toLowerCase()];
        return base ? `${base}?displayIds=${encodeURIComponent(idsCsv)}` : "";
    }

    if (downloadSelectedButton && selectedDownloadType && !downloadSelectedButton.dataset.bound) {
        downloadSelectedButton.dataset.bound = "1";
        let busySelected = false;

        downloadSelectedButton.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (busySelected) return;
            if (!selectedIds.size || !selectedDownloadType.value) return;

            busySelected = true;
            downloadSelectedButton.disabled = true;
            if (selectedProgressText) selectedProgressText.textContent = "Preparing…";

            const idsCsv = [...selectedIds].sort((a, b) => a - b).join(",");
            const url = getSelectedDownloadUrl(selectedDownloadType.value, idsCsv);
            if (url) triggerBrowserDownload(url);

            setTimeout(() => {
                busySelected = false;
                if (selectedProgressText) selectedProgressText.textContent = "";
                setSelectedUi();
            }, 1500);
        });
    }

    // ============================================================
    // Init
    // ============================================================
    loadDatasetsForDropdown();
});
