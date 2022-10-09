let fnDict = {}
let id = 0

function genFnId() {
    return 'f' + id++
}

function serialize(eventType, e) {
    if (eventType.includes('key'))
        return serializeKeyboardEvent(e)
    else if (eventType.includes('mouse'))
        return serializeMouseEvent(e)
    else if (eventType.includes('paste'))
        return serializeClipboardEvent(e)
}

function serializeKeyboardEvent(e) {
    if (e) {
        return {
            key: e.key,
            code: e.keyCode.toString(),
            location: e.location,
            repeat: e.repeat,
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            altKey: e.altKey,
            metaKey: e.metaKey,
            type: e.type
        };
    }
}

function serializeMouseEvent(e) {
    if (e) {
        return {
            button: e.button,
            buttons: e.buttons,
            clientX: e.clientX,
            clientY: e.clientY,
            ctrlKey: e.ctrlKey,
            shiftKey: e.shiftKey,
            metaKey: e.metaKey,
            offsetX: e.offsetX,
            offsetY: e.offsetY,
            pageX: e.pageX,
            screenX: e.screenX,
            screenTop: e.screenY
        }
    }
}

function serializeClipboardEvent(e) {
    if (e) {
        if (e.clipboardData && e.clipboardData.getData) {
            let pasteText = ""
            try {
                pasteText = e.clipboardData.getData('text/plain')
            } catch (ex) {
                pasteText = ""
            }
            return {
                text: pasteText
            }
        }
    }
    return {
        text: "",
    }
}

window.setupBlazorWindowEvent = async function (dotNetHelper, evType, handlerName) {
    let fn = async (ev) => {
        let response = await dotNetHelper.invokeMethodAsync(handlerName, serialize(evType, ev))
        if (response == true) {
            ev.preventDefault()
            ev.stopImmediatePropagation()
        }
    }
    window.addEventListener(evType, fn)
    let id = genFnId()
    fnDict[id] = fn
    return id
}

window.removeBlazorWindowEvent = function (evType, fnId) {
    window.removeEventListener(evType, fnDict[fnId])
}