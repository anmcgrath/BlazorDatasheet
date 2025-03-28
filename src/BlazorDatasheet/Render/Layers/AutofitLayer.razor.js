/**
 *
 * @param {HTMLElement} el
 */
export function measure(el) {
    if (el) {
        console.log(el.offsetWidth)
        return {
            width: el.offsetWidth,
            height: el.offsetHeight
        }
    }
}