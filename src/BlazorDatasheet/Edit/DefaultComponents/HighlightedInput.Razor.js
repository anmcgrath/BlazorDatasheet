class Highligher {
    #inputEl;
    #highlightResultEl;

    constructor(options) {
        if (!options.inputEl)
            return

        this.#inputEl = options.inputEl
        this.#inputEl.innerText = options.initialText
        this.#highlightResultEl = options.highlightResultEl
        this.#highlightResultEl.innerHTML = options.initialHtml

        this.#inputEl.addEventListener('input', e => {
            if (!options.dotnetHelper)
                return
            options.dotnetHelper.invokeMethodAsync("UpdateInput", e.target.innerText)
        })

        const moveCursorToEnd = function (el) {
            const range = document.createRange();
            const selection = window.getSelection();
            range.setStart(el, el.childNodes.length);
            range.collapse(true);
            selection.removeAllRanges();
            selection.addRange(range);
        };

        const focusAndMoveCursorToEnd = function () {
            options.inputEl.focus()
            moveCursorToEnd(options.inputEl)
        }

        setTimeout(focusAndMoveCursorToEnd, 0);
    }

    setHighlightHtml(html) {
        this.#highlightResultEl.innerHTML = html
    }

}

export function createHighlighter(el, highlightEl, dotnetHelper) {
    return new Highligher(el, highlightEl, dotnetHelper)
}