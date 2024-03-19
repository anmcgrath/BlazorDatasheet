/**
 * @property {number} sheetX
 * @property {number} sheetY
 */
class SheetPointerEventArgs {
    sheetX;
    sheetY;
    row;
    col;
}

class PointerInputService {
    pointerEnterCallbackName;
    pointerDoubleClickCallbackName;

    /**
     *
     * @param {HTMLElement} sheetElement
     * @param dotnetHelper
     * @param {string} pointerUpCallbackName
     * @param {string} pointerDownCallbackName
     * @param {string} pointerMoveCallbackName
     * @param {string} pointerEnterCallbackName
     * @param {string} pointerDoubleClickCallbackName
     * @param {string} pointerEnterCallbackName
     * @param {string} pointerDoubleClickCallbackName
     */
    constructor(sheetElement,
                dotnetHelper,
                pointerUpCallbackName,
                pointerDownCallbackName,
                pointerMoveCallbackName,
                pointerEnterCallbackName,
                pointerDoubleClickCallbackName) {
        this.sheetElement = sheetElement;
        this.pointerUpCallbackName = pointerUpCallbackName;
        this.pointerDownCallbackName = pointerDownCallbackName;
        this.pointerMoveCallbackName = pointerMoveCallbackName;
        this.dotnetHelper = dotnetHelper
        this.pointerEnterCallbackName = pointerEnterCallbackName;
        this.pointerDoubleClickCallbackName = pointerDoubleClickCallbackName;

        this.sheetElement.addEventListener('pointerup', this.onPointerUp.bind(this));
        this.sheetElement.addEventListener('pointerdown', this.onPointerDown.bind(this));
        this.sheetElement.addEventListener('pointermove', this.onPointerMove.bind(this));
        this.sheetElement.addEventListener('dblclick', this.onDoubleClick.bind(this));
        
        this.currentRow = -1
        this.currentCol = -1
    }

    /**
     *
     * @param {PointerEvent} e
     */
    onPointerUp(e) {
        this.dotnetHelper.invokeMethodAsync(this.pointerUpCallbackName, this.getSheetPointerEventArgs(e));
    }

    onPointerDown(e) {
        console.log(e)
        this.dotnetHelper.invokeMethodAsync(this.pointerDownCallbackName, this.getSheetPointerEventArgs(e));
    }

    onPointerMove(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (args.row !== this.currentRow || args.col !== this.currentCol) {
            this.onCellEnter(args)
        }

        this.currentRow = args.row
        this.currentCol = args.col

        this.dotnetHelper.invokeMethodAsync(this.pointerMoveCallbackName, args);
    }

    onDoubleClick(e) {
        let args = this.getSheetPointerEventArgs(e)
        this.dotnetHelper.invokeMethodAsync(this.pointerDoubleClickCallbackName, args);
    }

    onCellEnter(args) {
        this.dotnetHelper.invokeMethodAsync(this.pointerEnterCallbackName, args);
    }


    /**
     * @param {MouseEvent} e
     */
    getSheetPointerEventArgs(e) {
        let rect = this.sheetElement.getBoundingClientRect();
        let x = e.clientX - rect.x;
        let y = e.clientY - rect.y;
        let targetClassList = e.target.classList;
        let row, col = -1
        let cell = e.target.closest('.sheet-cell')
        if (cell) {
            row = parseInt(cell.dataset.row)
            col = parseInt(cell.dataset.col)
        }

        return {
            sheetX: x,
            sheetY: y,
            row: row,
            col: col,
            altKey: e.altKey,
            ctrlKey: e.ctrlKey,
            metaKey: e.metaKey,
            shiftKey: e.shiftKey,
        };
    }

}

export function getInputService(sheetElement, dotnetHelper, pointerUpCallbackName, pointerDownCallbackName, pointerMoveCallbackName, pointerEnterCallbackName, pointerDoubleClickCallbackName) {
    return new PointerInputService(sheetElement, dotnetHelper, pointerUpCallbackName, pointerDownCallbackName, pointerMoveCallbackName, pointerEnterCallbackName, pointerDoubleClickCallbackName);
}