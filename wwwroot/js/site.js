document.addEventListener('DOMContentLoaded', function () {
    const downloadTypeDropdown = document.getElementById('downloadType');
    const downloadButton = document.getElementById('downloadButton');
    let isDownloading = false; // Prevent multiple downloads

    if (downloadTypeDropdown && downloadButton) {
        // Enable/disable download button based on selection
        downloadTypeDropdown.addEventListener('change', function () {
            downloadButton.disabled = !this.value;
            updateButtonState('Download', 'btn-primary');
        }); 

        // Handle download button click
        downloadButton.addEventListener('click', async function (event) {
            event.preventDefault();
            if (isDownloading) return;

            const selectedValue = downloadTypeDropdown.value;
            if (!selectedValue) return;

            isDownloading = true;
            downloadButton.disabled = true; // Disable the button after first click
            updateButtonState('0%', 'btn-primary');

            await startDownload(selectedValue);
        });
    }

    async function startDownload(selectedValue) {
        const url = getDownloadUrl(selectedValue);
        if (!url) return;

        await simulateProgress();

        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Network error');
            const blob = await response.blob();

            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = `metadata.${selectedValue}`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(link.href);

            updateButtonState('Download Complete', 'btn-success');
        } catch (error) {
            alert('Download failed, please try again.');
            console.error(error);
            updateButtonState('Download Failed', 'btn-danger');
        } finally {
            setTimeout(() => resetButtonState(), 1500);
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

    function getDownloadUrl(selectedValue) {
        return {
            csv: '/Download/DownloadCsv',
            pdf: '/Download/DownloadPdf',
            json: '/Download/DownloadJson',
            xlsx: '/Download/DownloadXlsx'
        }[selectedValue] || '';
    }

    function updateButtonState(text, className) {
        downloadButton.textContent = text;
        downloadButton.className = `btn ${className}`;
    }

    function resetButtonState() {
        isDownloading = false;
        downloadButton.disabled = false; // Enable the button after download
        updateButtonState('Download', 'btn-primary');
    }
});





document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('download-template').addEventListener('click', function (event) {
        event.preventDefault(); // Prevent default behavior

        // Fetch the file and trigger download using FileSaver.js
        fetch("/contribution_files/metadata_template.xlsx")
            .then(response => response.blob())
            .then(blob => {
                saveAs(blob, "metadata_template.xlsx");
            })
            .catch(error => console.error('Error downloading the file:', error));
    });

    document.getElementById('download-guidelines').addEventListener('click', function (event) {
        event.preventDefault(); // Prevent default behavior

        // Fetch the file and trigger download using FileSaver.js
        fetch("/contribution_files/guidelines.docx")
            .then(response => response.blob())
            .then(blob => {
                saveAs(blob, "guidelines.docx");
            })
            .catch(error => console.error('Error downloading the file:', error));
    });
});