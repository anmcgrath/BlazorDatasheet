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
        const isScrollable = (v) => v !== 'visible' && v !== 'clip'
        if (isScrollable(style.overflowY) || isScrollable(style.overflowX) || isScrollable(style.overflow))
            return parent
        parent = parent.parentElement
    }

    return null
}
