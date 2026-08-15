class Highligher {
    #inputEl;
    #highlightResultEl;
    #caretToEndPending = false;
    #onFocusMoveCaret;

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
            // Replacing textContent destroys the current selection, so the caret must always be restored.
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
            if (document.activeElement !== el) {
                // Focus hasn't landed yet - some webviews (e.g. WebView2 under MAUI) apply focus()
                // on a later turn of the message loop. Defer instead of silently giving up, otherwise
                // the caret is left at offset 0 and typed text ends up in front of the existing text.
                this.deferCursorToEnd(el)
                return
            }

            const range = document.createRange();
            const selection = document.getSelection();

            // selectNodeContents works whether or not the element has any child nodes yet.
            range.selectNodeContents(el)
            range.collapse(false);
            selection.removeAllRanges();
            selection.addRange(range);
        };

        this.deferCursorToEnd = function (el) {
            if (this.#caretToEndPending)
                return

            this.#caretToEndPending = true
            this.#onFocusMoveCaret = () => {
                el.removeEventListener('focus', this.#onFocusMoveCaret)
                this.#onFocusMoveCaret = undefined
                this.#caretToEndPending = false
                this.moveCursorToEnd(el)
            }
            el.addEventListener('focus', this.#onFocusMoveCaret)
        }

        this.cancelDeferredCursorToEnd = function () {
            if (!this.#onFocusMoveCaret)
                return

            this.#inputEl.removeEventListener('focus', this.#onFocusMoveCaret)
            this.#onFocusMoveCaret = undefined
            this.#caretToEndPending = false
        }

        this.focusAndMoveCursorToEnd = function () {
            options.inputEl.focus()

            if (document.activeElement !== options.inputEl) {
                // Retry once on the next frame - a webview may not have been able to take focus yet.
                requestAnimationFrame(() => {
                    options.inputEl.focus()
                    this.moveCursorToEnd(options.inputEl)
                })
                return
            }

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
        // The user is placing the caret themselves - don't yank it to the end when focus lands.
        this.cancelDeferredCursorToEnd()
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
            if (this.#onFocusMoveCaret) {
                this.#inputEl.removeEventListener('focus', this.#onFocusMoveCaret)
                this.#onFocusMoveCaret = undefined
            }
        }
        this.resizeObserver.disconnect()
        document.removeEventListener('selectionchange', this.updateCaretPosition)
    }

}

export function createHighlighter(el, highlightEl, dotnetHelper) {
    return new Highligher(el, highlightEl, dotnetHelper)
}
