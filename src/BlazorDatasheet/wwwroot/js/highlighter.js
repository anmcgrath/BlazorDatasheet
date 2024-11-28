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
            console.log('input set', e.target.innerText)
            console.log('caret position = ', this.getCaretPosition())
            options.dotnetHelper.invokeMethodAsync("UpdateInput", e.target.innerText)
        })

        ///Returns caret position if selection is inside highligher, otherwise -1
        this.getCaretPosition = function () {
            let sel = window.getSelection()
            if(sel == null || sel.focusNode == null)
                return -1
            
            let isSelectionInside = sel.focusNode.parentElement === options.inputEl
            if (!isSelectionInside)
                return -1
            let len = sel.toString().length
            let caretPosition = -1
            if (len === 0)
                caretPosition = sel.focusOffset
            return caretPosition
        }

        this.updateCaretPosition = function () {
            options.dotnetHelper.invokeMethodAsync("UpdateCaretPosition", this.getCaretPosition())
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

        if (options.focusOnInit) {
            setTimeout(this.focusAndMoveCursorToEnd, 0);
        }

        //document.addEventListener('selectionchange', this.updateCaretPosition)
    }


    setHighlightHtml(html) {
        this.#highlightResultEl.innerHTML = html
    }

    createRange = (node, targetPosition) => {
        let range = document.createRange();
        range.selectNode(node);
        range.setStart(node, 0);

        let pos = 0;
        const stack = [node];
        while (stack.length > 0) {
            const current = stack.pop();

            if (current.nodeType === Node.TEXT_NODE) {
                const len = current.textContent.length;
                if (pos + len >= targetPosition) {
                    range.setEnd(current, targetPosition - pos);
                    return range;
                }
                pos += len;
            } else if (current.childNodes && current.childNodes.length > 0) {
                for (let i = current.childNodes.length - 1; i >= 0; i--) {
                    stack.push(current.childNodes[i]);
                }
            }
        }

        // The target position is greater than the
        // length of the contenteditable element.
        range.setEnd(node, node.childNodes.length);
        return range;
    };

    setInnerText(text) {
        const cursorPosition = this.getCaretPosition()
        this.#inputEl.innerText = text;

        if(cursorPosition < 0)
            return
        
        const setrange = this.createRange(this.#inputEl, cursorPosition);

        const setselection = window.getSelection();
        setrange.collapse(false)
        console.log(setrange)
        setselection.removeAllRanges();
        setselection.addRange(setrange);
        
    }

    dispose() {
        document.removeEventListener('selectionchange', this.updateCaretPosition)
    }

}

export function createHighlighter(el, highlightEl, dotnetHelper) {
    return new Highligher(el, highlightEl, dotnetHelper)
}