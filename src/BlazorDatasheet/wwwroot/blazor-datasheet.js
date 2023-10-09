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

// Adds a window event and stores the function as a unique ID
// The reason we do this rather than adding one window event is so that
// we can remove the events later
window.setupBlazorWindowEvent = async function (dotNetHelper, evType, dotnetHandlerName) {
    let fn = async (ev) => {
        // The response from calling the .net function
        let isHandledResponse = await dotNetHelper.invokeMethodAsync(dotnetHandlerName, serialize(evType, ev))
        if (isHandledResponse == true) {
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


// Mouse move events
function onThrottledMouseMove(component, interval) {
    window.addEventListener('mousemove', e => {
        component.invokeMethodAsync('HandleMouseMove', e.pageX, e.pageY);
    }, interval);
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

function findScrollableAncestor(element) {
    let parent = element.parentElement

    if (parent == null)
        return document
    let overflowY = window.getComputedStyle(parent).overflowY
    let overflowX = window.getComputedStyle(parent).overflowX
    let overflow = window.getComputedStyle(parent).overflow

    if (overflowY == 'scroll' || overflowX == 'scroll' || overflow == 'scroll')
        return parent
    return findScrollableAncestor(parent)
}

function serializeScrollEvent(target, el) {
    let r = el.getBoundingClientRect()
    let t = target == document.documentElement ? {
        top: 0,
        left: 0,
        height: window.innerHeight,
        width: window.innerWidth
    } : target.getBoundingClientRect()
    console.log("el", r, "target", t)
    return {
        scrollTop: Math.max(0, t.top - r.top),
        scrollHeight: r.height,
        scrollWidth: r.width,
        scrollLeft: Math.max(0, t.left - r.left),
        containerHeight: t.height - Math.max(0, r.top - t.top),
        containerWidth: t.width - Math.max(0, r.left - t.left)
    }
}

function getScrollOffsetSizes(el, parent) {
    // how much of the element has disappeared above the parent's scroll area?
    // if the parent is an element, it is equal to scroll height
    // otherwise if the parent is the document, it is equal to the top of the document - top of element.
    let docRect = document.documentElement.getBoundingClientRect()
    let elRect = el.getBoundingClientRect()

    let scrollTop = parent === document ? Math.max(0, -elRect.top) : parent.scrollTop
    let scrollLeft = parent === document ? Math.max(0, -elRect.left) : parent.scrollLeft

    // if the parent is the document, the client height is the visible height of the element in the window
    // otherwise it is the height of the parent
    let clientHeight = parent === document ? window.innerHeight : parent.clientHeight
    let clientWidth = parent === document ? window.innerWidth : parent.clientWidth

    // scroll height/width is always the height/width of the element
    let scrollHeight = elRect.height
    let scrollWidth = elRect.width

    return {
        scrollWidth: scrollWidth,
        scrollHeight: scrollHeight,
        scrollLeft: scrollLeft,
        scrollTop: scrollTop,
        containerWidth: clientWidth,
        containerHeight: clientHeight
    }
}


window.addScrollListener = async function (dotNetHelper, el, dotnetHandlerName) {
    // return initial scroll event to render the sheet
    let parent = findScrollableAncestor(el)
    dotNetHelper.invokeMethodAsync(dotnetHandlerName, getScrollOffsetSizes(el, parent))
    parent.onscroll = throttle(async function (ev) {
        let offset = getScrollOffsetSizes(el, parent)
        let isHandledResponse = await dotNetHelper.invokeMethodAsync(dotnetHandlerName, offset)
        if (isHandledResponse == true) {
            ev.preventDefault()
            ev.stopImmediatePropagation()
        }
    }, 100)
}