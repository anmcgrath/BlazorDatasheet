window.writeTextToClipboard = async function (text) {
    await window.navigator.clipboard.writeText(text)
}

window.setFocusWithTimeout = function (el, timeout) {
    setTimeout(() => {
        el.focus()
    }, timeout)
}

window.setContextListener = function (el, dotnetHelper, handlerName) {
    el.addEventListener('contextmenu', async e => {
        console.log(e.target)
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.hasAttribute("contentEditable"))
            return

        e.preventDefault()
        await dotnetHelper.invokeMethodAsync(handlerName, {clientX: e.clientX, clientY: e.clientY})
    })
}