export class AutoScroller {

    findScrollableAncestor(element) {
        if (!element)
            return null

        let parent = element.parentElement

        if (parent == null || element === document.body || element === document.documentElement)
            return null

        let overflowY = window.getComputedStyle(parent).overflowY
        let overflowX = window.getComputedStyle(parent).overflowX
        let overflow = window.getComputedStyle(parent).overflow

        if (overflowY !== 'visible' || overflowX !== 'visible' || overflow !== 'visible')
            return parent

        return this.findScrollableAncestor(parent)
    }

    scrollBy(x, y) {
        if (x === 0 && y === 0)
            return
        this.ancestor.scrollBy({top: y, left: x, behavior: 'smooth'})
    }

    /**
     *
     * @param {HTMLElement} el
     * @param dotnetHelper
     */
    subscribe(el, dotnetHelper) {
        this.dotnetHelper = dotnetHelper
        this.ancestor = this.findScrollableAncestor(el) ?? document.documentElement
        this._throttledMouseMove = this.throttle(this.onMouseMove.bind(this), 20)
        window.addEventListener('mousemove', this._throttledMouseMove)
    }

    /***
     * @param {MouseEvent} e
     */
    onMouseMove(e) {
        if (!this.dotnetHelper)
            return

        let rect = this.ancestor.getBoundingClientRect()
        let insideX = rect.left < e.clientX && rect.right > e.clientX
        let insideY = rect.top < e.clientY && rect.bottom > e.clientY

        if (insideX && insideY) {
            if (this._outsideAncestor) {
                this._outsideAncestor = false
                this.dotnetHelper.invokeMethodAsync("HandleMouseOutsideOfScrollableAncestor", {x: 0, y: 0})
            }
            return
        }

        this._outsideAncestor = true
        let edgeX = e.clientX > rect.right ? rect.right : rect.left
        let edgeY = e.clientY > rect.bottom ? rect.bottom : rect.top
        let dx = insideX ? 0 : e.clientX - edgeX
        let dy = insideY ? 0 : e.clientY - edgeY

        this.dotnetHelper.invokeMethodAsync("HandleMouseOutsideOfScrollableAncestor", {x: dx, y: dy})
    }

    dispose() {
        if (this._throttledMouseMove)
            window.removeEventListener('mousemove', this._throttledMouseMove)
        this.dotnetHelper = null
    }

    throttle(mainFunction, delay) {
        let timerFlag = null;
        return (...args) => {
            if (timerFlag === null) {
                mainFunction(...args);
                timerFlag = setTimeout(() => {
                    timerFlag = null;
                }, delay);
            }
        };
    }
}

export function createAutoScroller() {
    return new AutoScroller()
}