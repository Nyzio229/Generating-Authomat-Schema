// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function openImageInNewWindow(imageUrl) {
    // Otwieranie nowego okna o wymiarach 600x400px
    window.open(imageUrl, "_blank", "width=600,height=400,resizable=yes");
}

document.addEventListener('shown.bs.modal', function (event) {
    const modal = event.target;
    const apiResponse = document.getElementById('modalImage').dataset.apiResponse;
    const downloadLink = modal.querySelector('.btn-success');

    // Ustaw dynamiczny link
    if (downloadLink && apiResponse) {
        downloadLink.href = `/GenerateMachine?handler=DownloadJflap&apiResponse=${encodeURIComponent(apiResponse)}`;
    }
});