document.addEventListener('DOMContentLoaded', function () {
    const downloadTypeDropdown = document.getElementById('downloadType');
    const downloadButton = document.getElementById('downloadButton');
    const templateDownloadBtn = document.getElementById('download-template');
    const guidelinesDownloadBtn = document.getElementById('download-guidelines');
    const logoutForm = document.getElementById('logoutForm');
    let isDownloading = false;

    if (logoutForm) {
        logoutForm.addEventListener('submit', function (e) {
            const confirmLogout = confirm("Are you sure you want to log out?");
            if (!confirmLogout) {
                e.preventDefault();
            }
        });
    }

    if (downloadTypeDropdown && downloadButton) {
        downloadTypeDropdown.addEventListener('change', () => {
            downloadButton.disabled = !downloadTypeDropdown.value;
            updateButtonState('Download', 'btn-primary');
        });

        downloadButton.addEventListener('click', async (event) => {
            event.preventDefault();
            if (isDownloading || !downloadTypeDropdown.value) return;

            isDownloading = true;
            downloadButton.disabled = true;
            updateButtonState('0%', 'btn-primary');

            await handleDownload(downloadTypeDropdown.value);
        }, { once: true });
    }

    if (templateDownloadBtn) {
        templateDownloadBtn.addEventListener('click', (event) => {
            event.preventDefault();
            fetchAndDownloadSingle("/contribution_files/metadata_template.xlsx", "metadata_template.xlsx");
        });
    }

    if (guidelinesDownloadBtn) {
        guidelinesDownloadBtn.addEventListener('click', (event) => {
            event.preventDefault();
            fetchAndDownloadSingle("/contribution_files/guidelines.docx", "guidelines.docx");
        });
    }

    async function handleDownload(format) {
        const url = getDownloadUrl(format);
        if (!url) return;

        await simulateProgress();

        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Download failed');

            const blob = await response.blob();
            const objectUrl = URL.createObjectURL(blob);

            const link = document.createElement('a');
            link.href = objectUrl;
            link.download = `metadata.${format}`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(objectUrl);

            updateButtonState('Download Complete', 'btn-success');
        } catch (err) {
            console.error(err);
            alert('Download failed, please try again.');
            updateButtonState('Download Failed', 'btn-danger');
        } finally {
            setTimeout(resetButtonState, 1500);
        }
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
        return {
            csv: '/Download/DownloadCsv',
            pdf: '/Download/DownloadPdf',
            json: '/Download/DownloadJson',
            xlsx: '/Download/DownloadXlsx'
        }[format] || '';
    }

    function updateButtonState(text, className) {
        if (downloadButton) {
            downloadButton.textContent = text;
            downloadButton.className = `btn ${className}`;
        }
    }

    function resetButtonState() {
        isDownloading = false;
        if (downloadButton) {
            downloadButton.disabled = false;
            updateButtonState('Download', 'btn-primary');
        }
    }

    function fetchAndDownloadSingle(url, filename) {
        fetch(url)
            .then(response => {
                if (!response.ok) throw new Error('Fetch failed');
                return response.blob();
            })
            .then(blob => {
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                link.download = filename;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                URL.revokeObjectURL(link.href);
            })
            .catch(error => console.error(`Error downloading ${filename}:`, error));
    }
});


