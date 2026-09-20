/**
 * Measures the currently rendered autofit batch and returns one size candidate per affected
 * row or column. Raw per-cell measurements never cross the JS interop boundary.
 *
 * @param {HTMLElement} el
 * @param {"row" | "col"} axis
 */
export function measureAutofitChanges(el, axis) {
    if (!el) {
        return []
    }

    const measureWidth = axis === "col"
    const changes = new Map()
    let measuredCount = 0

    for (const child of el.children) {
        if (measuredCount++ === 200) {
            break
        }

        const index = Number.parseInt(child.dataset.axisIndex, 10)
        const axisSize = Number.parseFloat(child.dataset.axisSize)
        const cellSize = Number.parseFloat(child.dataset.cellSize)

        if (!Number.isFinite(index) || !Number.isFinite(axisSize) || !Number.isFinite(cellSize)) {
            continue
        }

        const measuredSize = measureWidth ? measureFullWidth(child) : child.offsetHeight
        let change = changes.get(index)

        if (!change) {
            change = {index}
            changes.set(index, change)
        }

        // Express both expansion and contraction as the size of the affected axis after
        // applying the measured delta. This also handles merged cells, where cellSize can be
        // larger than axisSize.
        const targetSize = Math.max(0, axisSize + measuredSize - cellSize)
        if (targetSize > axisSize) {
            change.expandTo = change.expandTo === undefined
                ? targetSize
                : Math.max(change.expandTo, targetSize)
        } else {
            // Keep exact-fit candidates. Otherwise an exact-fit widest cell is discarded and
            // a smaller cell can contract the axis, forcing the widest cell to expand it again
            // on the next autofit.
            change.contractTo = change.contractTo === undefined
                ? targetSize
                : Math.max(change.contractTo, targetSize)
        }
    }

    return Array.from(changes.values()).filter(change =>
        change.expandTo !== undefined || change.contractTo !== undefined)
}

/**
 * offsetWidth rounds to the nearest pixel, which can leave the column a fraction of a pixel too
 * narrow. A number that doesn't quite fit loses a digit or is replaced with hashes, so the width
 * is always rounded up.
 *
 * getBoundingClientRect gives the subpixel border-box width, which is what offsetWidth rounds.
 * getComputedStyle would resolve the content box instead, and reading it costs a style
 * recalculation per element on top of the layout this already forces.
 *
 * @param {HTMLElement} el
 */
function measureFullWidth(el) {
    const width = el.getBoundingClientRect().width
    return Number.isFinite(width) && width > 0 ? Math.ceil(width) : el.offsetWidth
}
