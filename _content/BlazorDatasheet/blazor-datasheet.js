// import eagerly to avoid lag when first edit occurs.
import("./js/highlighter.js");

window.writeTextToClipboard = async function (text) {
    if (window.isSecureContext) {
        await window.navigator.clipboard.writeText(text)
    } else {
        console.log("Copy failed as window was not considered a secure context.")
    }

}

window.setFocusWithTimeout = function (el, timeout) {
    setTimeout(() => {
        el.focus()
    }, timeout)
}