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
        let parent = this.findScrollableAncestor(wholeEl) || document.documentElement
        let wholeSheetRect = wholeEl.getBoundingClientRect()

        // find the position within the grid where the content should start from
        // if the el top left is visible, left and top should be <= 0
        // if the el left is less than the container edge it should be the distance past the left
        // edge that the el top left corner is (this is given by the parent scrollLeft/scrollTop IF 
        // THERE IS NOTHING BETWEEN IT AND THE SCROLL CONTAINER
        // which is why we remove the offsetTop/offsetLeft

        // if position is sticky, the offset top and left are always zero.

        // need to find the offset from the next scrollable element and include that in the scroll calculation


        let offsetLeft = this.getOffsetLeftToScrollableParent(wholeEl)
        let offseTTop = this.getOffsetTopToScrollableParent(wholeEl)

        let top = parent.scrollTop - offseTTop
        let left = parent.scrollLeft - offsetLeft
        let width = parent === document.documentElement ? window.innerWidth : parent.clientWidth
        let height = parent === document.documentElement ? window.innerHeight : parent.clientHeight
        width += 1
        height += 1


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

    isInsideSticky(el) {
        if (el == null || el.parentElement == null)
            return false
        let parentStyle = window.getComputedStyle(el.parentElement)
        return parentStyle.position === 'sticky' || this.isInsideSticky(el.parentElement)
    }

    isInsideStickyLeft(el) {
        if (!el.parentElement)
            return false
        let parentStyle = window.getComputedStyle(el.parentElement)
        return parentStyle.position !== 'static' && parentStyle.left !== 'auto'
    }

    isInsideStickyTop(el) {
        if (!el.parentElement)
            return false
        let parentStyle = window.getComputedStyle(el.parentElement)
        return parentStyle.position !== 'static' && parentStyle.top !== 'auto'
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

    scrollTo(wholeSheetEl, x, y, behaviour) {
        let parent = this.findScrollableAncestor(wholeSheetEl) || document.documentElement
        let offsetLeft = this.getOffsetLeftToScrollableParent(wholeSheetEl)
        let offseTTop = this.getOffsetTopToScrollableParent(wholeSheetEl)

        parent.scrollTo({left: x + offsetLeft, top: y + offseTTop, behavior: behaviour})
    }

    sheetMousePositionListeners = {}
}


export function getVirtualiser() {
    return new Virtualiser2d()
}