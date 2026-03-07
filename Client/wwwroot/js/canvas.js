// Canvas interop helpers
window.getElementBounds = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return { left: 0, top: 0 };
    const rect = el.getBoundingClientRect();
    return { left: rect.left, top: rect.top };
};
