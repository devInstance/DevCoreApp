function blazor_setTitle(title: string): void {
    document.title = title;
}

function blazor_getCulture(): string | null {
    return localStorage.getItem("BlazorCulture");
}

function blazor_setCulture(value: string): void {
    localStorage.setItem("BlazorCulture", value);
}

function showBootstrapModal(id: string): boolean {
    const theModal = new bootstrap.Modal("#" + id, {
        keyboard: true,
        focus: true,
    });
    theModal.show();
    return true;
}

function dismissBootstrapModal(id: string): boolean {
    const element = document.getElementById(id);
    if (!element) return false;
    const modal = bootstrap.Modal.getInstance(element);
    if (modal) modal.hide();
    return true;
}

function downloadFileFromBytes(fileName: string, contentType: string, bytes: ArrayLike<number>): void {
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

window.showBootstrapModal = showBootstrapModal;
window.dismissBootstrapModal = dismissBootstrapModal;
window.downloadFileFromBytes = downloadFileFromBytes;
