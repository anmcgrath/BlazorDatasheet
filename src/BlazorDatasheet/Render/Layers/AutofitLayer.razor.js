/**
 *
 * @param {HTMLElement} el
 */
export function measureElement(el) {
    if (el) {
        console.log(el.offsetWidth)
        return {
            width: el.offsetWidth,
            height: el.offsetHeight
        }
    }
}