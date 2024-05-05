

window.writeTextToClipboard = async function (text) {
    await window.navigator.clipboard.writeText(text)
}

window.setFocusWithTimeout = function (el, timeout) {
    setTimeout(() => {
        el.focus()
    }, timeout)
}