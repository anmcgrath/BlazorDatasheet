import { watchRemoval } from "./removal-watcher.js"

/**
 * @property {number} sheetX
 * @property {number} sheetY
 */
class SheetPointerEventArgs {
    sheetX;
    sheetY;
    pageX;
    pageY;
    row;
    col;
    altKey;
    ctrlKey;
    shiftKey;
    metaKey;
}

class PointerInputService {
    pointerEnterCallbackName;
    pointerDoubleClickCallbackName;

    constructor(sheetElement, dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.sheetElement = sheetElement;
        this.currentRow = -1
        this.currentCol = -1
        this.pointerMoveEnabled = false
        this.onPointerUpHandler = this.onPointerUp.bind(this)
        this.onPointerDownHandler = this.onPointerDown.bind(this)
        this.onDoubleClickHandler = this.onDoubleClick.bind(this)
        this.onPointerMoveHandler = this.onPointerMove.bind(this)
        this.registered = false
    }

    /**
     * Turns the pointer-move callback into .NET on or off.
     * @param {boolean} enabled Whether anything is listening for pointer move.
     * @returns {void}
     */
    setPointerMoveEnabled(enabled) {
        this.pointerMoveEnabled = !!enabled
    }

    registerPointerEvents(pointerUpCallbackName, pointerDownCallbackName, pointerMoveCallbackName, pointerEnterCallbackName, pointerDoubleClickCallbackName) {
        if (this.registered) return
        this.pointerUpCallbackName = pointerUpCallbackName;
        this.pointerDownCallbackName = pointerDownCallbackName;
        this.pointerMoveCallbackName = pointerMoveCallbackName;
        this.pointerEnterCallbackName = pointerEnterCallbackName;
        this.pointerDoubleClickCallbackName = pointerDoubleClickCallbackName;

        this.sheetElement.addEventListener('pointerup', this.onPointerUpHandler);
        this.sheetElement.addEventListener('pointerdown', this.onPointerDownHandler);
        this.sheetElement.addEventListener('dblclick', this.onDoubleClickHandler);
        this.sheetElement.addEventListener('pointermove', this.onPointerMoveHandler);
        this.registered = true
        this.unwatchRemoval = watchRemoval(this.sheetElement, () => this.dispose())
    }

    onPointerUp(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerUpCallbackName, args);
    }

    onPointerDown(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return
        this.dotnetHelper.invokeMethodAsync(this.pointerDownCallbackName, args);
    }

    onPointerMove(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        if (args.row !== this.currentRow || args.col !== this.currentCol) {
            this.onCellEnter(args)
        }

        this.currentRow = args.row
        this.currentCol = args.col

        if (this.pointerMoveEnabled)
            this.dotnetHelper.invokeMethodAsync(this.pointerMoveCallbackName, args);
    }

    onDoubleClick(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerDoubleClickCallbackName, args);
    }

    onCellEnter(args) {
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerEnterCallbackName, args);
    }

    dispose() {
        this.unwatchRemoval?.()
        this.unwatchRemoval = null
        if (!this.registered) {
            this.dotnetHelper = null
            return
        }
        this.registered = false
        this.sheetElement.removeEventListener('pointerup', this.onPointerUpHandler);
        this.sheetElement.removeEventListener('pointerdown', this.onPointerDownHandler);
        this.sheetElement.removeEventListener('pointermove', this.onPointerMoveHandler);
        this.sheetElement.removeEventListener('dblclick', this.onDoubleClickHandler);
        this.dotnetHelper = null
    }


    /**
     * @param {MouseEvent} e
     * @returns {SheetPointerEventArgs}
     */
    getSheetPointerEventArgs(e) {
        let rect = this.sheetElement.getBoundingClientRect();
        let x = e.clientX - rect.x;
        let y = e.clientY - rect.y;
        let targetClassList = e.target.classList;
        let row, col = -1
        let cell = e.target.closest('.bds-sheet-cell')

        if (!cell)
            return null

        if (cell && cell.dataset.row && cell.dataset.col) {
            row = parseInt(cell.dataset.row)
            col = parseInt(cell.dataset.col)
        }

        return {
            sheetX: x,
            sheetY: y,
            pageX: e.pageX,
            pageY: e.pageY,
            row: row,
            col: col,
            altKey: e.altKey,
            ctrlKey: e.ctrlKey,
            metaKey: e.metaKey,
            shiftKey: e.shiftKey,
            mouseButton: e.button
        };
    }

}

/**
 * @param sheetElement
 * @param dotnetHelper
 * @param {string[]} [callbackNames] When given, the pointer events are registered in this same
 * call, so creating the service costs one interop round trip rather than two.
 */
export function getInputService(sheetElement, dotnetHelper, callbackNames) {
    const service = new PointerInputService(sheetElement, dotnetHelper);
    if (callbackNames)
        service.registerPointerEvents(...callbackNames);
    return service;
}
