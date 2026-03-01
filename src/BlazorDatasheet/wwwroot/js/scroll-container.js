import { findScrollableAncestor } from "./scroll-utils.js"

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

/**
 * Scrolls the nearest scrollable container for the supplied element.
 * @param {number} x Horizontal scroll delta in pixels.
 * @param {number} y Vertical scroll delta in pixels.
 * @param {HTMLElement} el Element whose scrollable ancestor should be moved.
 * @returns {void}
 */
export function scrollParentBy(x, y, el) {
    let parent = findScrollableAncestor(el) || document.documentElement
    parent.scrollBy({left: x, top: y, behavior: "auto"})
}
