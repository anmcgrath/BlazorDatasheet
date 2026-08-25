/**
 * Tracks an autofill drag and reports the pointer position, in layer coordinates, to .NET.
 *
 * The position is measured against a live getBoundingClientRect() of an element sitting at the
 * layer origin.
 */
class DragTracker {

    /**
     * @param {HTMLElement} originElement Element positioned at the layer origin.
     * @param dotnetHelper
     * @param {string} moveCallbackName Invoked with {x, y} in layer coordinates.
     * @param {string} upCallbackName Invoked with no arguments when the drag ends.
     */
    start(originElement, dotnetHelper, moveCallbackName, upCallbackName) {
        this.stop()

        this.originElement = originElement
        this.dotnetHelper = dotnetHelper
        this.moveCallbackName = moveCallbackName
        this.upCallbackName = upCallbackName
        this.lastClient = null

        this._onPointerMove = this.throttle((e) => {
            this.lastClient = {x: e.clientX, y: e.clientY}
            this.emit()
        }, 25)

        this._onPointerUp = () => {
            const helper = this.dotnetHelper
            const callbackName = this.upCallbackName
            this.stop()
            if (helper)
                helper.invokeMethodAsync(callbackName)
        }

        this._onScroll = () => this.emit()

        window.addEventListener('pointermove', this._onPointerMove)
        window.addEventListener('pointerup', this._onPointerUp)
        window.addEventListener('pointercancel', this._onPointerUp)
        // captured on the window so that whichever ancestor actually scrolls is picked up
        window.addEventListener('scroll', this._onScroll, {capture: true, passive: true})
    }

    emit() {
        if (!this.dotnetHelper || !this.originElement || !this.lastClient)
            return

        const rect = this.originElement.getBoundingClientRect()
        this.dotnetHelper.invokeMethodAsync(this.moveCallbackName, {
            x: this.lastClient.x - rect.left,
            y: this.lastClient.y - rect.top
        })
    }

    stop() {
        if (this._onPointerMove) {
            window.removeEventListener('pointermove', this._onPointerMove)
            window.removeEventListener('pointerup', this._onPointerUp)
            window.removeEventListener('pointercancel', this._onPointerUp)
            window.removeEventListener('scroll', this._onScroll, {capture: true})
        }

        this._onPointerMove = null
        this._onPointerUp = null
        this._onScroll = null
        this.dotnetHelper = null
        this.originElement = null
        this.lastClient = null
    }

    dispose() {
        this.stop()
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

export function createDragTracker() {
    return new DragTracker()
}
