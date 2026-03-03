function blazor_setTitle(title) {
    document.title = title;
}
function blazor_getCulture() {
    return localStorage['BlazorCulture'];
}
function blazor_setCulture(value) {
    localStorage['BlazorCulture'] = value;
}
;
function showBootstrapModal(id) {
    var theModal = new bootstrap.Modal('#' + id, {
        keyboard: true,
        focus: true
    });
    theModal.show();
    return true;
}
function dismissBootstrapModal(id) {
    var theModal = document.getElementById(id);
    var modal = bootstrap.Modal.getInstance(theModal);
    modal.hide();
    return true;
}
window.showBootstrapModal = showBootstrapModal;
window.dismissBootstrapModal = dismissBootstrapModal;
function downloadFileFromBytes(fileName, contentType, bytes) {
    var blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
window.downloadFileFromBytes = downloadFileFromBytes;
//# sourceMappingURL=app.js.map