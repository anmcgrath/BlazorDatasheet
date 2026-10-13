let globalsConfigured = false

/**
 * Configures global helpers required by BlazorDatasheet.
 * @returns {void}
 */
export function setupGlobals() {
    if (globalsConfigured)
        return

    globalsConfigured = true

    // Import eagerly to avoid lag when first edit occurs.
    import("./js/highlighter.js")

    window.writeTextToClipboard = async function (text) {
        if (window.isSecureContext) {
            await window.navigator.clipboard.writeText(text)
        } else {
            console.log("Copy failed as window was not considered a secure context.")
        }
    }

    /**
     * Writes text and, where the browser allows it, HTML to the system clipboard.
     * @returns {Promise<boolean>} whether anything was written
     */
    window.bdsWriteClipboard = async function (text, html) {
        if (!window.isSecureContext || !navigator.clipboard) {
            console.log("Copy failed as window was not considered a secure context.")
            return false
        }
        try {
            if (html && navigator.clipboard.write && window.ClipboardItem) {
                const item = new ClipboardItem({
                    'text/plain': new Blob([text ?? ''], {type: 'text/plain'}),
                    'text/html': new Blob([html], {type: 'text/html'})
                })
                await navigator.clipboard.write([item])
                return true
            }
        } catch (e) {
            console.debug("BlazorDatasheet: rich clipboard write failed, falling back to text", e)
        }
        try {
            await navigator.clipboard.writeText(text ?? '')
            return true
        } catch (e) {
            console.warn("BlazorDatasheet: clipboard write failed", e)
            return false
        }
    }

    /**
     * Reads text and HTML from the system clipboard. Needs a user gesture and, in some browsers, permission.
     * @returns {Promise<{text: string, html: string}|null>} null when the clipboard could not be read
     */
    window.bdsReadClipboard = async function () {
        if (!window.isSecureContext || !navigator.clipboard)
            return null
        try {
            if (navigator.clipboard.read) {
                const items = await navigator.clipboard.read()
                let text = '', html = ''
                for (const item of items) {
                    if (!html && item.types.includes('text/html'))
                        html = await (await item.getType('text/html')).text()
                    if (!text && item.types.includes('text/plain'))
                        text = await (await item.getType('text/plain')).text()
                }
                return {text, html}
            }
        } catch (e) {
            console.debug("BlazorDatasheet: rich clipboard read failed, falling back to text", e)
        }
        try {
            return {text: await navigator.clipboard.readText(), html: ''}
        } catch (e) {
            return null
        }
    }

    /**
     * Encodes clipboard content for .NET to read as an IJSStreamReference, which is sent in chunks: a single
     * interop message that large would exceed the Blazor Server limit on messages from the browser and close
     * the circuit. Return it from a function .NET invokes for an IJSStreamReference.
     * @returns {Uint8Array} the content as UTF-8 JSON
     */
    window.bdsClipboardStream = function (data) {
        return new TextEncoder().encode(JSON.stringify(data))
    }

    /**
     * As bdsReadClipboard, returning the content as a stream (see bdsClipboardStream).
     */
    window.bdsReadClipboardStream = async function () {
        const data = await window.bdsReadClipboard()
        return data ? window.bdsClipboardStream(data) : null
    }

    window.setFocusWithTimeout = function (el, timeout) {
        setTimeout(() => {
            el.focus()
        }, timeout)
    }
}

// Supports older and newer Blazor start hooks.
export function beforeStart() {
    setupGlobals()
}

export function beforeWebStart() {
    setupGlobals()
}

export function afterStarted() {
}

export function afterWebStarted() {
}
