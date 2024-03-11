class Highligher {
    #inputEl;
    #highlightResultEl;

    constructor(options) {
        if (!options.inputEl)
            return

        let self = this
        this.#inputEl = options.inputEl
        this.#inputEl.innerText = options.initialText
        this.#highlightResultEl = options.highlightResultEl
        this.#highlightResultEl.innerHTML = options.initialHtml

        this.#inputEl.addEventListener('input', e => {
            if (!options.dotnetHelper)
                return
            options.dotnetHelper.invokeMethodAsync("UpdateInput", e.target.innerText)
        })


        this.updateCaretPosition = function () {
            let sel = window.getSelection()
            let isSelectionInside = sel.focusNode.parentElement === options.inputEl
            let len = sel.toString().length
            let caretPosition = -1
            if (isSelectionInside && len === 0)
                caretPosition = sel.focusOffset
            options.dotnetHelper.invokeMethodAsync("UpdateCaretPosition", caretPosition)
        }

        const moveCursorToEnd = function (el) {
            const range = document.createRange();
            const selection = window.getSelection();
            range.setStart(el, el.childNodes.length);
            range.collapse(true);
            selection.removeAllRanges();
            selection.addRange(range);
        };

        this.focusAndMoveCursorToEnd = function () {
            options.inputEl.focus()
            moveCursorToEnd(options.inputEl)
        }

        setTimeout(this.focusAndMoveCursorToEnd, 0);

        document.addEventListener('selectionchange', this.updateCaretPosition)
    }


    setHighlightHtml(html) {
        this.#highlightResultEl.innerHTML = html
    }

    dispose() {
        document.removeEventListener('selectionchange', this.updateCaretPosition)
    }

}

export function createHighlighter(el, highlightEl, dotnetHelper) {
    return new Highligher(el, highlightEl, dotnetHelper)
}