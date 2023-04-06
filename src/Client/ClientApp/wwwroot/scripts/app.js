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
//# sourceMappingURL=app.js.map