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

        const measuredSize = measureWidth ? child.offsetWidth : child.offsetHeight
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
