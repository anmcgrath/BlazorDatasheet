class Highligher {
    #inputEl;
    #highlightResultEl;
    #caretToEndPending = false;
    // where the caret goes when the input next takes focus. null is the end of the text.
    #pendingCaret = null;
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

        this.setInputText = function (text, caret = null) {
            // Replacing textContent destroys the current selection, so the caret must always be restored.
            this.#inputEl.textContent = text
            this.moveCursorTo(this.#inputEl, caret)
        }

        // The number of characters between the start of the input and a position in the DOM.
        this.textOffsetOf = function (node, offset) {
            const range = document.createRange()
            range.selectNodeContents(options.inputEl)
            range.setEnd(node, offset)
            return range.toString().length
        }

        // Reports the text selection, or -1 when the selection is somewhere else. The selection is
        // what decides where a reference picked from the sheet goes.
        this.updateCaretPosition = function () {
            let sel = window.getSelection()
            if (!sel?.focusNode)
                return

            let start = -1
            let end = -1
            // A blurred input can still hold the document's selection, e.g. in Firefox after a click on
            // the sheet. Changing its text then moves that selection, which isn't the user moving the caret.
            if (document.activeElement === options.inputEl &&
                options.inputEl.contains(sel.anchorNode) && options.inputEl.contains(sel.focusNode)) {
                const anchor = self.textOffsetOf(sel.anchorNode, sel.anchorOffset)
                const focus = self.textOffsetOf(sel.focusNode, sel.focusOffset)
                start = Math.min(anchor, focus)
                end = Math.max(anchor, focus)
            }

            self.invoke("HandleSelectionUpdate", start, end)
        }

        this.moveCursorToEnd = function (el) {
            this.moveCursorTo(el, null)
        }

        // Moves the caret to a text position, or to the end of the text if the position is null.
        this.moveCursorTo = function (el, caret) {
            if (document.activeElement !== el) {
                // Focus hasn't landed yet - some webviews (e.g. WebView2 under MAUI) apply focus()
                // on a later turn of the message loop. Defer instead of silently giving up, otherwise
                // the caret is left at offset 0 and typed text ends up in front of the existing text.
                this.#pendingCaret = caret
                this.deferCursorToEnd(el)
                return
            }

            const range = document.createRange();
            const selection = document.getSelection();

            // selectNodeContents works whether or not the element has any child nodes yet.
            range.selectNodeContents(el)
            range.collapse(false);

            if (caret != null && caret >= 0) {
                let remaining = caret
                const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT)
                let node
                while ((node = walker.nextNode())) {
                    if (remaining <= node.length) {
                        range.setStart(node, remaining)
                        range.collapse(true)
                        break
                    }
                    remaining -= node.length
                }
            }

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
                this.moveCursorTo(el, this.#pendingCaret)
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

        // Takes focus back after the sheet had it, leaving the caret where the text was last changed.
        this.focusAndMoveCursorTo = function (caret) {
            if (this.#disposed) return;
            this.cancelDeferredCursorToEnd()
            options.inputEl.focus()
            this.moveCursorTo(options.inputEl, caret)
        }

        this.focusAndMoveCursorToEnd = function (onlyIfWithinSheet = false) {
            const sheet = options.inputEl.closest('.bds-sheet');
            // A sheet activated from code without browser focus leaves the active element on body,
            // and the editor may still take focus from there. An external control keeps it.
            const canFocus = () => !this.#disposed && (!onlyIfWithinSheet || !sheet ||
                (document.hasFocus() && (sheet.contains(document.activeElement) ||
                    document.activeElement === document.body)));
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

    invoke(method, ...args) {
        if (this.#disposed || !this.options.dotnetHelper) return;
        return this.options.dotnetHelper.invokeMethodAsync(method, ...args).catch(error => {
            if (!this.#disposed) console.error('Datasheet editor event failed', error);
        });
    }

    onResize(e) {

    }

    onKeyDown(e) {
        // An editor outside the sheet hands these keys to the sheet, which finishes the edit. They must
        // never reach the input: enter would add a line and tab would move focus before the sheet takes it.
        if (this.options.preventAcceptKeys && !e.isComposing && (e.key === "Enter" || e.key === "Tab"))
            e.preventDefault()

        // While a list of suggestions is open these keys work the list, which is handled in .NET.
        if (this.options.captureListKeys && !e.isComposing &&
            (e.key === "Enter" || e.key === "Tab" || e.key === "ArrowUp" || e.key === "ArrowDown"))
            e.preventDefault()

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

    setCaptureListKeys(capture) {
        this.options.captureListKeys = capture
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
