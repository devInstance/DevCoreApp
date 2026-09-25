"use strict";
function blazor_setTitle(title) {
    document.title = title;
}
function blazor_getCulture() {
    return localStorage.getItem("BlazorCulture");
}
function blazor_setCulture(value) {
    localStorage.setItem("BlazorCulture", value);
}
function showBootstrapModal(id) {
    const theModal = new bootstrap.Modal("#" + id, {
        keyboard: true,
        focus: true,
    });
    theModal.show();
    return true;
}
function dismissBootstrapModal(id) {
    const element = document.getElementById(id);
    if (!element)
        return false;
    const modal = bootstrap.Modal.getInstance(element);
    if (modal)
        modal.hide();
    return true;
}
function downloadFileFromBytes(fileName, contentType, bytes) {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
async function copyTextToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    }
    catch {
        return false;
    }
}
window.showBootstrapModal = showBootstrapModal;
window.dismissBootstrapModal = dismissBootstrapModal;
window.downloadFileFromBytes = downloadFileFromBytes;
window.copyTextToClipboard = copyTextToClipboard;
//# sourceMappingURL=app.js.map