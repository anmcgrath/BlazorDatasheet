class Virtualizer {

    findScrollableAncestor(element) {
        let parent = element.parentElement

        if (parent == null || element == document.body || element == document.documentElement)
            return null

        let overflowY = window.getComputedStyle(parent).overflowY
        let overflowX = window.getComputedStyle(parent).overflowX
        let overflow = window.getComputedStyle(parent).overflow

        if (overflowY == 'scroll' || overflowX == 'scroll' || overflow == 'scroll')
            return parent
        return this.findScrollableAncestor(parent)
    }

    getViewportInfo(wholeSheetEl) {
        let parent = this.findScrollableAncestor(wholeSheetEl) || document.documentElement
        // how much of the element has disappeared above the parent's scroll area?
        // if the parent is an element, it is equal to scroll height
        // otherwise if the parent is the document, it is equal to the top of the document - top of element.
        let wholeSheetRect = wholeSheetEl.getBoundingClientRect()

        let scrollTop = parent === document.documentElement ? Math.max(0, -wholeSheetRect.top) : parent.scrollTop
        let scrollLeft = parent === document.documentElement ? Math.max(0, -wholeSheetRect.left) : parent.scrollLeft

        // if the parent is the document, the client height is the visible height of the element in the window
        // otherwise it is the height of the parent
        let clientHeight = parent === document.documentElement ? window.innerHeight : parent.clientHeight
        let clientWidth = parent === document.documentElement ? window.innerWidth : parent.clientWidth

        // scroll height/width is always the height/width of the element
        let scrollHeight = wholeSheetRect.height
        let scrollWidth = wholeSheetRect.width
        
        return {
            scrollLeft: scrollLeft,
            scrollTop: scrollTop,
            containerWidth: clientWidth,
            containerHeight: clientHeight
        }
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

    addVirtualisationHandlers(dotNetHelper, el, dotnetScrollHandlerName, fillerLeft, fillerTop, fillerRight, fillerBottom) {

        // return initial scroll event to render the sheet
        let parent = this.findScrollableAncestor(el)
        if (parent) {
            parent.style.willChange = 'transform' // improves scrolling performance in chrome/edge
        }

        // fixes scroll jankiness with chrome and firefox.
        (parent ?? document.documentElement).style.overflowAnchor = 'none'

        let getViewPort = this.getViewportInfo.bind(this)

        let offset = getViewPort(el)
        dotNetHelper.invokeMethodAsync(dotnetScrollHandlerName, offset);

        let self = this
        let observer = new IntersectionObserver((entries, observer) => {
            for (let i = 0; i < entries.length; i++) {
                if (!entries[i].isIntersecting)
                    continue
                if (entries[i].target.getBoundingClientRect().width <= 0 ||
                    entries[i].target.getBoundingClientRect().height <= 0)
                    continue

                let offset = getViewPort(el)
                dotNetHelper.invokeMethodAsync(dotnetScrollHandlerName, offset);
            }

        }, {root: parent, threshold: 0})

        observer.observe(fillerTop)
        observer.observe(fillerBottom)
        observer.observe(fillerLeft)
        observer.observe(fillerRight)

        this.interactionMap[el] = observer

        this.topHandlerMutMap[el] = this.createMutationObserver(fillerTop, observer)
        this.bottomHandlerMutMap[el] = this.createMutationObserver(fillerBottom, observer)
        this.leftHandlerMutMap[el] = this.createMutationObserver(fillerLeft, observer)
        this.rightHandlerMutMap[el] = this.createMutationObserver(fillerRight, observer)

    }

    /***
     * @param {DOMRect} rect1
     * @param {DOMRect} rect2
     * @returns {DOMRect}
     */
    rectIntersection(rect1, rect2) {
        let left = Math.max(rect1.left, rect2.left)
        let right = Math.min(rect1.right, rect2.right)
        if (right <= left)
            return null
        let top = Math.max(rect1.top, rect2.top)
        let bottom = Math.min(rect1.bottom, rect2.bottom)
        return new DOMRect(left, top, right - left, bottom - top)
    }

    dotNetHelperMap = {}

    createMutationObserver(filler, interactionObserver) {
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

    sheetMousePositionListeners = {}
}

export function

getVirtualizer() {
    return new Virtualizer()
}