class Virtualiser2d {

    findScrollableAncestor(element) {
        if (!element)
            return null

        let parent = element.parentElement
        while (parent != null && parent !== document.body && parent !== document.documentElement) {
            let style = window.getComputedStyle(parent)
            if (style.overflowY !== 'visible' || style.overflowX !== 'visible' || style.overflow !== 'visible')
                return parent
            parent = parent.parentElement
        }

        return null
    }

    disposeVirtualisationHandlers(el) {
        let left = this.leftHandlerMutMap.get(el)
        let right = this.rightHandlerMutMap.get(el)
        let top = this.topHandlerMutMap.get(el)
        let bottom = this.bottomHandlerMutMap.get(el)
        let interaction = this.interactionMap.get(el)

        left?.disconnect()
        right?.disconnect()
        top?.disconnect()
        bottom?.disconnect()
        interaction?.disconnect()

        this.leftHandlerMutMap.delete(el)
        this.rightHandlerMutMap.delete(el)
        this.topHandlerMutMap.delete(el)
        this.bottomHandlerMutMap.delete(el)
        this.interactionMap.delete(el)
    }

    leftHandlerMutMap = new WeakMap()
    rightHandlerMutMap = new WeakMap()
    topHandlerMutMap = new WeakMap()
    bottomHandlerMutMap = new WeakMap()
    interactionMap = new WeakMap()

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

        let observer = new IntersectionObserver((entries, observer) => {
            let shouldNotify = false
            for (let i = 0; i < entries.length; i++) {
                if (!entries[i].isIntersecting)
                    continue

                let rect = entries[i].target.getBoundingClientRect()
                if (rect.width <= 0 || rect.height <= 0)
                    continue

                shouldNotify = true
                break
            }

            if (!shouldNotify)
                return

            let viewRect = getRect(wholeEl)
            if (dotNetHelper)
                dotNetHelper.invokeMethodAsync(dotnetScrollHandlerName, viewRect);
        }, {root: parent, threshold: 0})

        observer.observe(fillerTop)
        observer.observe(fillerBottom)
        observer.observe(fillerLeft)
        observer.observe(fillerRight)

        this.interactionMap.set(wholeEl, observer)

        this.topHandlerMutMap.set(wholeEl, this.createMutationObserver(fillerTop, observer))
        this.bottomHandlerMutMap.set(wholeEl, this.createMutationObserver(fillerBottom, observer))
        this.leftHandlerMutMap.set(wholeEl, this.createMutationObserver(fillerLeft, observer))
        this.rightHandlerMutMap.set(wholeEl, this.createMutationObserver(fillerRight, observer))

    }

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

}


export function getVirtualiser() {
    return new Virtualiser2d()
}
