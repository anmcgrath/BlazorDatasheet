class Highligher {
    #inputEl;
    #highlightResultEl;

    constructor(options) {
        if (!options.inputEl)
            return

        this.options = options;

        let self = this
        this.#inputEl = options.inputEl
        this.#inputEl.textContent = options.initialText
        this.#highlightResultEl = options.highlightResultEl
        this.#highlightResultEl.innerHTML = options.initialHtml

        this.#inputEl.addEventListener('keydown', this.onKeyDown.bind(this))
        this.#inputEl.addEventListener('mousedown', this.onMouseDown.bind(this))

        this.#inputEl.addEventListener('input', e => {
            if (!options.dotnetHelper)
                return

            options.dotnetHelper.invokeMethodAsync("HandleInput", e.target.textContent)
        })

        this.resizeObserver = new ResizeObserver((entries) => {
            for (const entry of entries) {
                if (entry.target === this.#inputEl) {
                    options.dotnetHelper.invokeMethodAsync("HandleInputSizeChanged", entry.target.getBoundingClientRect())
                }
            }
        })
        this.resizeObserver.observe(this.#inputEl)

        this.setInputText = function (text) {
            this.#inputEl.textContent = text
            this.moveCursorToEnd(this.#inputEl)
        }

        this.updateCaretPosition = function () {
            let sel = window.getSelection()
            let isSelectionInside = sel.focusNode.parentElement === options.inputEl ||
                sel.focusNode === options.inputEl
            let len = sel.toString().length
            let caretPosition = -1

            if (isSelectionInside && len === 0)
                caretPosition = sel.focusOffset

            options.dotnetHelper.invokeMethodAsync("HandleCaretPositionUpdate", caretPosition)
        }

        this.moveCursorToEnd = function (el) {
            if (document.activeElement !== el)
                return
            if (el.childNodes.length === 0)
                return

            const range = document.createRange();
            const selection = document.getSelection();

            range.selectNodeContents(el.childNodes[0])
            range.setStart(el.childNodes[0], 0);
            range.setEnd(el.childNodes[0], el.textContent.length);
            range.collapse(false);
            selection.removeAllRanges();
            selection.addRange(range);
        };


        this.focusAndMoveCursorToEnd = function () {
            options.inputEl.focus()
            this.moveCursorToEnd(options.inputEl)
        }

        setTimeout(this.focusAndMoveCursorToEnd.bind(this), 0);

        document.addEventListener('selectionchange', this.updateCaretPosition)
    }

    onResize(e) {

    }

    onKeyDown(e) {
        if (!this.options.preventDefaultArrowKeys)
            return

        if (e.key === "Enter")
            e.preventDefault()

        if (e.key.startsWith('Arrow')) {
            e.preventDefault()
        }
    }

    onMouseDown() {
        this.options.preventDefaultArrowKeys = false
    }

    cancelPreventDefault() {
        this.options.preventDefaultArrowKeys = false
    }

    setHighlightHtml(html) {
        this.#highlightResultEl.innerHTML = html
    }

    dispose() {
        if (this.#inputEl) {
            this.#inputEl.removeEventListener('keydown', this.onKeyDown)
            this.#inputEl.removeEventListener('mousedown', this.onMouseDown)
        }
        this.resizeObserver.disconnect()
        document.removeEventListener('selectionchange', this.updateCaretPosition)
    }

}

export function createHighlighter(el, highlightEl, dotnetHelper) {
    return new Highligher(el, highlightEl, dotnetHelper)
}