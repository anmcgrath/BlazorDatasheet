export function show(id) {
    document.getElementById(id)?.showPopover();
}

export function hide(id) {
    try { document.getElementById(id)?.hidePopover(); } catch {}
}
