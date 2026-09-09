class Highligher {
    #inputEl;
    #highlightResultEl;
    #caretToEndPending = false;
    #onFocusMoveCaret;
    #disposed = false;
    #onKeyDown;
    #onMouseDown;
    #onInput;

    constructor(options) {
        if (!options.inputEl)
            return

        this.options = options;

        let self = this
        this.#inputEl = options.inputEl
        this.#inputEl.textContent = options.initialText
        this.#highlightResultEl = options.highlightResultEl
        this.#highlightResultEl.innerHTML = options.initialHtml

        this.#onKeyDown = this.onKeyDown.bind(this)
        this.#onMouseDown = this.onMouseDown.bind(this)
        this.#onInput = e => this.invoke("HandleInput", e.target.textContent)
        this.#inputEl.addEventListener('keydown', this.#onKeyDown)
        this.#inputEl.addEventListener('mousedown', this.#onMouseDown)
        this.#inputEl.addEventListener('input', this.#onInput)

        this.resizeObserver = new ResizeObserver((entries) => {
            for (const entry of entries) {
                if (entry.target === this.#inputEl) {
                    this.invoke("HandleInputSizeChanged", entry.target.getBoundingClientRect())
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
            if (!sel?.focusNode)
                return
            let isSelectionInside = sel.focusNode.parentElement === options.inputEl ||
                sel.focusNode === options.inputEl
            let len = sel.toString().length
            let caretPosition = -1

            if (isSelectionInside && len === 0)
                caretPosition = sel.focusOffset

            self.invoke("HandleCaretPositionUpdate", caretPosition)
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

        this.focusAndMoveCursorToEnd = function (onlyIfWithinSheet = false) {
            const sheet = options.inputEl.closest('.bds-sheet');
            const canFocus = () => !this.#disposed && (!onlyIfWithinSheet || !sheet ||
                (document.hasFocus() && sheet.contains(document.activeElement)));
            if (!canFocus()) return;
            options.inputEl.focus()

            if (document.activeElement !== options.inputEl) {
                // Retry once on the next frame - a webview may not have been able to take focus yet.
                requestAnimationFrame(() => {
                    if (!canFocus()) return;
                    options.inputEl.focus()
                    this.moveCursorToEnd(options.inputEl)
                })
                return
            }

            this.moveCursorToEnd(options.inputEl)
        }

        // HighlightedInput requests focus after its latest initial value has reached the DOM.

        document.addEventListener('selectionchange', this.updateCaretPosition)
    }

    invoke(method, value) {
        if (this.#disposed || !this.options.dotnetHelper) return;
        return this.options.dotnetHelper.invokeMethodAsync(method, value).catch(error => {
            if (!this.#disposed) console.error('Datasheet editor event failed', error);
        });
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
        if (this.#disposed) return;
        this.#disposed = true;
        if (this.#inputEl) {
            this.#inputEl.removeEventListener('keydown', this.#onKeyDown)
            this.#inputEl.removeEventListener('mousedown', this.#onMouseDown)
            this.#inputEl.removeEventListener('input', this.#onInput)
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
