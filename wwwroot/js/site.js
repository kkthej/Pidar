document.addEventListener('DOMContentLoaded', function () {
    // Elements
    const downloadTypeDropdown = document.getElementById('downloadType');
    const downloadButton = document.getElementById('downloadButton');
    const templateDownloadBtn = document.getElementById('download-template');
    const guidelinesDownloadBtn = document.getElementById('download-guidelines');
    const logoutForm = document.getElementById('logoutForm');

    // State
    let isDownloading = false;
    let outsideClickListener = null;

    // Logout Toast Handler
    if (logoutForm) {
        logoutForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Create toast element
            const toastEl = document.createElement('div');
            toastEl.innerHTML = `
                <div class="toast-container position-fixed top-50 start-50 translate-middle p-3">
                    <div class="toast show" role="alert" aria-live="assertive" aria-atomic="true">
                        <div class="toast-header bg-white">
                            <strong class="me-auto">Logout Confirmation</strong>
                            <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                        </div>
                        <div class="toast-body text-center">
                            <p class="mb-3">Are you sure you want to log out?</p>
                            <div class="d-flex justify-content-center gap-2">
                                <button id="undoLogout" class="btn btn-success px-4">Cancel</button>
                                <button id="proceedLogout" class="btn btn-danger px-4">Logout</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(toastEl);

            // Focus management
            const undoBtn = toastEl.querySelector('#undoLogout');
            undoBtn?.focus();

            // Toast dismissal handler
            const dismissToast = () => {
                if (outsideClickListener) {
                    document.removeEventListener('click', outsideClickListener);
                }
                toastEl.classList.add('fade-out');
                setTimeout(() => {
                    toastEl.remove();
                }, 400);
            };

            // Event delegation for toast buttons
            toastEl.addEventListener('click', (event) => {
                if (event.target.id === 'undoLogout' || event.target.classList.contains('btn-close')) {
                    dismissToast();
                } else if (event.target.id === 'proceedLogout') {
                    dismissToast();
                    setTimeout(() => logoutForm.submit(), 400);
                }
            });

            // Close when clicking outside
            outsideClickListener = function (e) {
                if (!toastEl.contains(e.target) && e.target !== logoutForm) {
                    dismissToast();
                }
            };
            document.addEventListener('click', outsideClickListener);
        });
    }

    // Table Sorting Functionality
    function sortTableById() {
        const table = document.querySelector('table');
        if (!table) return;

        const tbody = table.querySelector('tbody');
        const rows = [...tbody.querySelectorAll('tr')];

        rows.sort((a, b) => {
            const aId = parseInt(a.cells[0].textContent);
            const bId = parseInt(b.cells[0].textContent);
            return aId - bId;
        });

        rows.forEach(row => tbody.appendChild(row));
    }

    // Enable/disable download button based on dropdown selection
    if (downloadTypeDropdown && downloadButton) {
        downloadTypeDropdown.addEventListener('change', function () {
            downloadButton.disabled = !this.value;
        });

        // Initialize button state
        downloadButton.disabled = !downloadTypeDropdown.value;
    }

    // Download Button Handler
    if (downloadButton && downloadTypeDropdown) {
        let lastClickTime = 0;

        downloadButton.addEventListener('click', async (event) => {
            event.preventDefault();
            event.stopPropagation();

            // Throttle rapid clicks
            const now = Date.now();
            if (now - lastClickTime < 1000) return;
            lastClickTime = now;

            if (isDownloading || !downloadTypeDropdown.value) return;

            isDownloading = true;
            downloadButton.disabled = true;
            updateButtonState('0%', 'btn-primary');

            try {
                await startDownload(downloadTypeDropdown.value);
            } catch (error) {
                console.error('Download error:', error);
                updateButtonState('Error', 'btn-danger');
            } finally {
                setTimeout(() => {
                    isDownloading = false;
                    resetButtonState();
                }, 1500);
            }
        });
    }

    // Template Download
    if (templateDownloadBtn) {
        templateDownloadBtn.addEventListener('click', (event) => {
            event.preventDefault();
            fetchAndDownload("/contribution_files/metadata_template.xlsx", "metadata_template.xlsx");
        });
    }

    // Guidelines Download
    if (guidelinesDownloadBtn) {
        guidelinesDownloadBtn.addEventListener('click', (event) => {
            event.preventDefault();
            fetchAndDownload("/contribution_files/guidelines.docx", "guidelines.docx");
        });
    }

    // Download Functions
    async function startDownload(format) {
        const url = getDownloadUrl(format);
        if (!url) throw new Error('Invalid download format');

        try {
            // Show progress
            await simulateProgress();

            // Fetch with timeout
            const controller = new AbortController();
            const timeout = setTimeout(() => controller.abort(), 30000);

            const response = await fetch(`${url}?t=${Date.now()}`, {
                signal: controller.signal
            });
            clearTimeout(timeout);

            if (!response.ok) throw new Error(`Server responded with ${response.status}`);

            // Check file size
            const contentLength = response.headers.get('Content-Length');
            if (contentLength && contentLength > 50000000) {
                if (!confirm('This file is quite large. Continue with download?')) {
                    throw new Error('Download cancelled by user');
                }
            }

            // Process download
            const blob = await response.blob();
            const filename = `metadata_${new Date().toISOString().slice(0, 10)}.${format}`;
            await downloadBlob(blob, filename);

            updateButtonState('Download Complete', 'btn-success');
        } catch (error) {
            updateButtonState('Download Failed', 'btn-danger');
            throw error;
        }
    }

    async function fetchAndDownload(url, filename) {
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error(`Failed to fetch ${filename}`);

            const blob = await response.blob();
            await downloadBlob(blob, filename);
        } catch (error) {
            console.error(`Error downloading ${filename}:`, error);
            alert(`Failed to download ${filename}. Please try again.`);
        }
    }

    async function downloadBlob(blob, filename) {
        return new Promise((resolve) => {
            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = filename;
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();

            setTimeout(() => {
                document.body.removeChild(link);
                URL.revokeObjectURL(link.href);
                resolve();
            }, 100);
        });
    }

    function simulateProgress() {
        return new Promise((resolve) => {
            let progress = 0;
            const interval = setInterval(() => {
                progress += 10;
                updateButtonState(`${progress}%`, 'btn-primary');
                if (progress >= 100) {
                    clearInterval(interval);
                    updateButtonState('Finalizing...', 'btn-success');
                    setTimeout(resolve, 500);
                }
            }, 300);
        });
    }

    function getDownloadUrl(format) {
        const validFormats = {
            csv: '/Download/DownloadCsv',
            pdf: '/Download/DownloadPdf',
            json: '/Download/DownloadJson',
            xlsx: '/Download/DownloadXlsx'
        };
        return validFormats[format.toLowerCase()] || '';
    }

    function updateButtonState(text, className) {
        if (downloadButton) {
            downloadButton.textContent = text;
            downloadButton.className = `btn ${className}`;
        }
    }

    function resetButtonState() {
        if (downloadButton) {
            downloadButton.disabled = false;
            updateButtonState('Download', 'btn-primary');
        }
    }
});