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
        this.rememberRelease = e => {
            this.pointerPressed = false
            this.lastRelease = e
        }
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

        window.addEventListener('pointerup', this.rememberRelease);
        window.addEventListener('pointercancel', this.rememberRelease);
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
        this.pointerPressed = true
        this.lastRelease = null
        // the whole grid sits in the context menu target, so only skip controls inside the sheet.
        // the sheet's own chrome (e.g. the heading dropdown) still selects its cell, but never starts a drag.
        const control = e.target.closest('button, .bds-sheet-menu-target')
        const isChrome = !!control?.hasAttribute('data-bds-chrome')
        if (control && !isChrome && this.sheetElement.contains(control)) return
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return
        args.isOnChrome = isChrome
        this.dotnetHelper.invokeMethodAsync(this.pointerDownCallbackName, args);
    }

    onPointerMove(e) {
        if (this.dragTracking) return
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

    setDragCursor(cursor) {
        if (!this.dragTracking) return
        this.sheetElement.dataset.bdsDragCursor = cursor
        this.sheetElement.style.cursor = cursor
    }

    setDragTracking(active) {
        if (!!active === !!this.dragTracking) return
        this.dragTracking = !!active
        if (active) {
            this.previousCursor = this.sheetElement.style.cursor
            this.dragMove = e => {
                this.lastDragPointer = e
                const args = this.getDragArgs(e)
                this.dotnetHelper?.invokeMethodAsync(this.pointerMoveCallbackName, args)
            }
            this.dragEnd = e => {
                const args = this.getDragArgs(e)
                args.isCancelled = e.type === 'pointercancel' || e.type === 'blur'
                this.setDragTracking(false)
                this.dotnetHelper?.invokeMethodAsync('HandleDragEnd', args)
            }
            this.dragScroll = () => {
                if (this.lastDragPointer) this.dragMove(this.lastDragPointer)
            }
            window.addEventListener('pointermove', this.dragMove)
            window.addEventListener('pointerup', this.dragEnd)
            window.addEventListener('pointercancel', this.dragEnd)
            window.addEventListener('blur', this.dragEnd)
            window.addEventListener('scroll', this.dragScroll, true)
            // A server round trip may finish after the user has already released a quick click.
            if (this.pointerPressed === false && this.lastRelease)
                this.dragEnd(this.lastRelease)
        } else {
            window.removeEventListener('pointermove', this.dragMove)
            window.removeEventListener('pointerup', this.dragEnd)
            window.removeEventListener('pointercancel', this.dragEnd)
            window.removeEventListener('blur', this.dragEnd)
            window.removeEventListener('scroll', this.dragScroll, true)
            this.lastDragPointer = null
            delete this.sheetElement.dataset.bdsDragCursor
            this.sheetElement.style.cursor = this.previousCursor ?? ''
        }
    }

    getDragArgs(e) {
        const cell = Number.isFinite(e.clientX) ? document.elementsFromPoint(e.clientX, e.clientY)
            .map(el => el.closest?.('.bds-sheet-cell[data-row][data-col]'))
            .find(el => el && this.sheetElement.contains(el)) : null
        if (cell) return this.getSheetPointerEventArgs(e, cell)
        return { row: -1, col: -1, pageX: e.pageX ?? 0, pageY: e.pageY ?? 0, isInsideSheet: false }
    }

    dispose() {
        this.setDragTracking(false)
        this.unwatchRemoval?.()
        this.unwatchRemoval = null
        if (!this.registered) {
            this.dotnetHelper = null
            return
        }
        this.registered = false
        window.removeEventListener('pointerup', this.rememberRelease);
        window.removeEventListener('pointercancel', this.rememberRelease);
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
    getSheetPointerEventArgs(e, targetCell = null) {
        let rect = this.sheetElement.getBoundingClientRect();
        let x = e.clientX - rect.x;
        let y = e.clientY - rect.y;

        let row, col = -1
        let cell = targetCell ?? e.target.closest?.('.bds-sheet-cell')

        if (!cell)
            return null

        if (cell && cell.dataset.row && cell.dataset.col) {
            row = parseInt(cell.dataset.row)
            col = parseInt(cell.dataset.col)
        }

        const cellRect = cell.getBoundingClientRect()
        return {
            isInsideSheet: true,
            rowFraction: cellRect.height ? (e.clientY - cellRect.top) / cellRect.height : 0,
            colFraction: cellRect.width ? (e.clientX - cellRect.left) / cellRect.width : 0,
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
