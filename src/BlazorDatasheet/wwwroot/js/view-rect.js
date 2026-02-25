/**
 * Finds the nearest ancestor that can scroll.
 * @param {HTMLElement | null} element Element to start from.
 * @returns {HTMLElement | null} The nearest scrollable ancestor, or null when none exists.
 */
export function findScrollableAncestor(element) {
    if (!element)
        return null

    let parent = element.parentElement
    while (parent != null && parent !== document.body && parent !== document.documentElement) {
        let style = window.getComputedStyle(parent)
        if (style.overflowY !== 'visible' || style.overflowX !== 'visible' || style.overflow !== 'visible')
            return parent
        parent = parent.parentElement
    }

    return null
}

/**
 * Calculates the visible viewport rectangle relative to the sheet root.
 * @param {HTMLElement | null} wholeEl Datasheet root element.
 * @param {(element: HTMLElement | null) => HTMLElement | null} [findScrollableAncestorFn] Scroll ancestor resolver.
 * @returns {{x:number, y:number, width:number, height:number} | null} Visible rect in sheet coordinates.
 */
export function calculateViewRect(wholeEl, findScrollableAncestorFn = findScrollableAncestor) {
    if (wholeEl == null)
        return null

    let parent = findScrollableAncestorFn(wholeEl) || document.documentElement
    let parentRect = parent === document.documentElement
        ? {top: 0, left: 0, bottom: window.innerHeight, right: window.innerWidth}
        : parent.getBoundingClientRect()

    let wholeRect = wholeEl.getBoundingClientRect()

    let top = (parentRect.top + (parent.clientTop || 0)) - wholeRect.top
    let left = (parentRect.left + (parent.clientLeft || 0)) - wholeRect.left
    let width = parent === document.documentElement ? window.innerWidth : parent.clientWidth
    let height = parent === document.documentElement ? window.innerHeight : parent.clientHeight

    return {
        x: left,
        y: top,
        width: width,
        height: height
    }
}
