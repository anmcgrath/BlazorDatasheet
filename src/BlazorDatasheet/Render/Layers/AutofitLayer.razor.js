/**
 *
 * @param {HTMLElement} el
 */
export function measureMaxChildrenDimensions(el) {
    if (el) {
        let results = []

        for (let child of el.children) {
            results.push({
                size: {
                    width: child.offsetWidth,
                    height: child.offsetHeight
                },
                row: parseInt(child.dataset.row),
                col: parseInt(child.dataset.col),
            })
        }
        return results
    }
}