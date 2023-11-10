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

    if (parent == null || element == document.body || element == document.documentElement)
        return null

    let overflowY = window.getComputedStyle(parent).overflowY
    let overflowX = window.getComputedStyle(parent).overflowX
    let overflow = window.getComputedStyle(parent).overflow

    if (overflowY == 'scroll' || overflowX == 'scroll' || overflow == 'scroll')
        return parent
    return findScrollableAncestor(parent)
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

window.disposeVirtualisationHandlers = function (el) {
    leftHandlerMutMap[el].disconnect()
    rightHandlerMutMap[el].disconnect()
    topHandlerMutMap[el].disconnect()
    bottomHandlerMutMap[el].disconnect()
    interactionMap[el].disconnect()

    leftHandlerMutMap[el] = {}
    rightHandlerMutMap[el] = {}
    topHandlerMutMap[el] = {}
    bottomHandlerMutMap[el] = {}
    interactionMap[el] = {}
}

leftHandlerMutMap = {}
rightHandlerMutMap = {}
topHandlerMutMap = {}
bottomHandlerMutMap = {}
interactionMap = {}

window.addVirtualisationHandlers = function (dotNetHelper, el, dotnetHandlerName, fillerLeft, fillerTop, fillerRight, fillerBottom) {

    // return initial scroll event to render the sheet
    let parent = findScrollableAncestor(el)
    if (parent) {
        parent.style.willChange = 'transform' // improves scrolling performance in chrome/edge
    }

    // fixes scroll jankiness with chrome and firefox.
    (parent ?? document.documentElement).style.overflowAnchor = 'none'

    let offset = getScrollOffsetSizes(el, parent || document.documentElement)
    dotNetHelper.invokeMethodAsync(dotnetHandlerName, offset);

    let observer = new IntersectionObserver((entries, observer) => {
        for (let i = 0; i < entries.length; i++) {
            if (!entries[i].isIntersecting)
                continue
            if (entries[i].target.getBoundingClientRect().width <= 0 ||
                entries[i].target.getBoundingClientRect().height <= 0)
                continue

            let offset = getScrollOffsetSizes(el, parent || document.documentElement)
            dotNetHelper.invokeMethodAsync(dotnetHandlerName, offset);
        }

    }, {root: parent, threshold: 0})

    observer.observe(fillerTop)
    observer.observe(fillerBottom)
    observer.observe(fillerLeft)
    observer.observe(fillerRight)

    interactionMap[el] = observer

    topHandlerMutMap[el] = createMutationObserver(fillerTop, observer)
    bottomHandlerMutMap[el] = createMutationObserver(fillerBottom, observer)
    leftHandlerMutMap[el] = createMutationObserver(fillerLeft, observer)
    rightHandlerMutMap[el] = createMutationObserver(fillerRight, observer)

    dotNetHelperMap = {}

}

last_page_posns_map = {}
resize_map = {}
dotNetHelperMap = {}

// adds listeners to determine when the scroll container is moved
// on the page, so that we can update the pageX and pageY coordinates stored for the element.
// these are used for determining row/column positions in mouse events
window.addPageMoveListener = function (dotNetHelper, el, dotnetFunctionName) {
    console.log('addPageMoveListener')
    let parent = findScrollableAncestor(el)
    if (!parent) parent = el
    // parent is scrollable, parent of parent is the container of the scroll.
    let resizeable = document.documentElement

    last_page_posns_map[el] = {pageX: getPageX(el), pageY: getPageY(el)}
    let res = new ResizeObserver((r, o) => {
        invokeIfPageXYNew(parent, dotNetHelper, dotnetFunctionName)
        //console.log(parent.getBoundingClientRect())
    })

    res.observe(resizeable)
    resize_map[el] = res
}

function createMutationObserver(filler, interactionObserver) {
    // if we are scrolling too fast (or rendering too slow) we may have a situation where
    // the filler elements get resized and end up in the observable scroll area which won't re-trigger
    // the interaction observer. so we add mutation observers to un-observe/re-observe the interaction
    // this is what asp.net/blazor's virtualize component does.
    let mutationObserver = new MutationObserver((m, o) => {
        interactionObserver.unobserve(filler)
        interactionObserver.observe(filler)
    })

    mutationObserver.observe(filler, {attributes: true})
    return mutationObserver
}