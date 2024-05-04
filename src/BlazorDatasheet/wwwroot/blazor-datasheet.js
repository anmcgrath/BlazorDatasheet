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
    else if (eventType.includes('scroll'))
        return serializeScrollEvent(e)
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
            screenY: e.screenY,
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

// Adds a window event and stores the function as a unique ID
// The reason we do this rather than adding one window event is so that
// we can remove the events later
window.setupBlazorWindowEvent = async function (dotNetHelper, evType, dotnetHandlerName) {
    let fn = async (ev) => {
        let isHandledResponse = await dotNetHelper.invokeMethodAsync(dotnetHandlerName, serialize(evType, ev))
        if (isHandledResponse === true) {
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

window.writeTextToClipboard = async function (text) {
    await window.navigator.clipboard.writeText(text)
}

window.setFocusWithTimeout = function (el, timeout) {
    setTimeout(() => {
        el.focus()
    }, timeout)
}

// https://stackoverflow.com/questions/27078285/simple-throttle-in-javascript
// Returns a function, that, when invoked, will only be triggered at most once
// during a given window of time. Normally, the throttled function will run
// as much as it can, without ever going more than once per `wait` duration;
// but if you'd like to disable the execution on the leading edge, pass
// `{leading: false}`. To disable execution on the trailing edge, ditto.
function throttle(func, wait, options) {
    var context, args, result;
    var timeout = null;
    var previous = 0;
    if (!options) options = {};
    var later = function () {
        previous = options.leading === false ? 0 : Date.now();
        timeout = null;
        result = func.apply(context, args);
        if (!timeout) context = args = null;
    };
    return function () {
        var now = Date.now();
        if (!previous && options.leading === false) previous = now;
        var remaining = wait - (now - previous);
        context = this;
        args = arguments;
        if (remaining <= 0 || remaining > wait) {
            if (timeout) {
                clearTimeout(timeout);
                timeout = null;
            }
            previous = now;
            result = func.apply(context, args);
            if (!timeout) context = args = null;
        } else if (!timeout && options.trailing !== false) {
            timeout = setTimeout(later, remaining);
        }
        return result;
    };
};

function createSheetMousePositionListener(dotnetHelper, innerSheetEl, dotnetHandlerName) {
    let f = throttle(e => {
        const rect = innerSheetEl.getBoundingClientRect()
        let x = e.clientX - rect.x
        let y = e.clientY - rect.y
        dotnetHelper.invokeMethodAsync(dotnetHandlerName,
            {
                x: x,
                y: y
            });
    }, 30)

    if (innerSheetEl) {
        window.addEventListener("mousemove", f)
        sheetMousePositionListeners[innerSheetEl] = f
    }
}

function removeSheetMousePositionListener(innerSheetEl) {
    if (sheetMousePositionListeners) {
        let v = sheetMousePositionListeners[innerSheetEl]
        window.removeEventListener("mousemove", v)
        delete sheetMousePositionListeners[innerSheetEl]
    }
}

function getRelativeMousePosition(el) {
    if (el) {
        let r = el.getBoundingClientRect()
        let d = currentClientPosition
        return {
            x: d.x - r.x,
            y: d.y - r.y
        }
    }

}