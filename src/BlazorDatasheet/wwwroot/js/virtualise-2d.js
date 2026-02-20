class Virtualiser2d {

    findScrollableAncestor(element) {
        if (!element)
            return null

        let parent = element.parentElement

        if (parent == null || element === document.body || element === document.documentElement)
            return null

        let overflowY = window.getComputedStyle(parent).overflowY
        let overflowX = window.getComputedStyle(parent).overflowX
        let overflow = window.getComputedStyle(parent).overflow

        if (overflowY !== 'visible' || overflowX !== 'visible' || overflow !== 'visible')
            return parent

        return this.findScrollableAncestor(parent)
    }

    disposeVirtualisationHandlers(el) {
        this.leftHandlerMutMap[el].disconnect()
        this.rightHandlerMutMap[el].disconnect()
        this.topHandlerMutMap[el].disconnect()
        this.bottomHandlerMutMap[el].disconnect()
        this.interactionMap[el].disconnect()

        this.leftHandlerMutMap[el] = {}
        this.rightHandlerMutMap[el] = {}
        this.topHandlerMutMap[el] = {}
        this.bottomHandlerMutMap[el] = {}
        this.interactionMap[el] = {}
    }

    leftHandlerMutMap = {}
    rightHandlerMutMap = {}
    topHandlerMutMap = {}
    bottomHandlerMutMap = {}
    interactionMap = {}
    resizeMap = {}

    calculateViewRect(wholeEl) {
        if (wholeEl == null)
            return null

        let parent = this.findScrollableAncestor(wholeEl) || document.documentElement
        let parentRect = parent === document.documentElement
            ? { top: 0, left: 0, bottom: window.innerHeight, right: window.innerWidth }
            : parent.getBoundingClientRect()

        let wholeRect = wholeEl.getBoundingClientRect()

        let top = (parentRect.top + (parent.clientTop || 0)) - wholeRect.top
        let left = (parentRect.left + (parent.clientLeft || 0)) - wholeRect.left
        let width = parent === document.documentElement ? window.innerWidth : parent.clientWidth
        let height = parent === document.documentElement ? window.innerHeight : parent.clientHeight

        let rect = {
            x: left,
            y: top,
            width: width,
            height: height
        }

        return rect
    }

    scrollParentBy(x, y, el) {
        let parent = this.findScrollableAncestor(el) || document.documentElement
        parent.scrollBy({left: x, top: y, behavior: "auto"})
    }

    getOffsetLeftToScrollableParent(el) {
        let offset = el.offsetLeft
        let parent = el.offsetParent

        while (parent !== null && !this.isScrollable(parent)) {
            offset += parent.offsetLeft
            parent = parent.offsetParent
        }
        return offset
    }

    getOffsetTopToScrollableParent(el) {
        let offset = el.offsetTop
        let parent = el.offsetParent

        while (parent !== null && !this.isScrollable(parent)) {
            offset += parent.offsetTop
            parent = parent.offsetParent
        }
        return offset
    }

    isScrollable(el) {
        let style = window.getComputedStyle(el)
        return style.overflow !== 'visible' || style.overflowX !== 'visible' || style.overflowY !== 'visible'
    }

    addVirtualisationHandlers(dotNetHelper, wholeEl, dotnetScrollHandlerName, fillerLeft, fillerTop, fillerRight, fillerBottom) {
        // return initial scroll event to render the sheet
        let parent = this.findScrollableAncestor(wholeEl)
        if (parent) {
            parent.style.willChange = 'transform' // improves scrolling performance in chrome/edge
        }

        // fixes scroll jankiness with chrome and firefox.
        (parent ?? document.documentElement).style.overflowAnchor = 'none'

        let getRect = this.calculateViewRect.bind(this)

        let viewRect = getRect(wholeEl)
        if (dotNetHelper)
            dotNetHelper.invokeMethodAsync(dotnetScrollHandlerName, viewRect);

        let self = this
        let observer = new IntersectionObserver((entries, observer) => {
            for (let i = 0; i < entries.length; i++) {
                if (!entries[i].isIntersecting)
                    continue

                if (entries[i].target.getBoundingClientRect().width <= 0 ||
                    entries[i].target.getBoundingClientRect().height <= 0)
                    continue

                let viewRect = getRect(wholeEl)
                if (dotNetHelper)
                    dotNetHelper.invokeMethodAsync(dotnetScrollHandlerName, viewRect);
            }

        }, {root: parent, threshold: 0})

        observer.observe(fillerTop)
        observer.observe(fillerBottom)
        observer.observe(fillerLeft)
        observer.observe(fillerRight)

        this.interactionMap[wholeEl] = observer

        this.topHandlerMutMap[wholeEl] = this.createMutationObserver(fillerTop, observer)
        this.bottomHandlerMutMap[wholeEl] = this.createMutationObserver(fillerBottom, observer)
        this.leftHandlerMutMap[wholeEl] = this.createMutationObserver(fillerLeft, observer)
        this.rightHandlerMutMap[wholeEl] = this.createMutationObserver(fillerRight, observer)

    }

    dotNetHelperMap = {}

    createMutationObserver(filler, interactionObserver) {
        // if we are scrolling too fast (or rendering too slow) we may have a situation where
        // the filler elements get resized and end up in the observable scroll area which won't re-trigger
        // the interaction observer. so we add mutation observers to un-observe/re-observe the interaction
        // this is what asp.net/blazor's virtualize component does.
        let mutationObserver = new ResizeObserver((m, o) => {
            interactionObserver.unobserve(filler)
            interactionObserver.observe(filler)
        })

        mutationObserver.observe(filler, {})
        return mutationObserver
    }

    sheetMousePositionListeners = {}
}


export function getVirtualiser() {
    return new Virtualiser2d()
}