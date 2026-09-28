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
// One window-focus callback into .NET (the notification bell). Passing a null target unregisters.
let focusListener = null;
function devcore_onWindowFocus(target, method) {
    if (focusListener) {
        window.removeEventListener("focus", focusListener);
        focusListener = null;
    }
    if (target && method) {
        focusListener = () => { void target.invokeMethodAsync(method); };
        window.addEventListener("focus", focusListener);
    }
}
window.showBootstrapModal = showBootstrapModal;
window.dismissBootstrapModal = dismissBootstrapModal;
window.downloadFileFromBytes = downloadFileFromBytes;
window.copyTextToClipboard = copyTextToClipboard;
window.devcore_onWindowFocus = devcore_onWindowFocus;
//# sourceMappingURL=app.js.map