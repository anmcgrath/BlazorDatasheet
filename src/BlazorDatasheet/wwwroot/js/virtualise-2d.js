import { calculateViewRect as calculateViewRectShared, findScrollableAncestor as findScrollableAncestorShared } from "./view-rect.js"

class Virtualiser2d {

    /**
     * Finds the nearest ancestor that can scroll.
     * @param {HTMLElement | null} element Element to start from.
     * @returns {HTMLElement | null} The nearest scrollable ancestor, or null when none exists.
     */
    findScrollableAncestor(element) {
        return findScrollableAncestorShared(element)
    }

    /**
     * Disconnects and removes all virtualisation observers attached to the element.
     * @param {HTMLElement} el Datasheet root element.
     * @returns {void}
     */
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

    /**
     * Calculates the visible viewport rectangle relative to the sheet root.
     * @param {HTMLElement | null} wholeEl Datasheet root element.
     * @returns {{x:number, y:number, width:number, height:number} | null} Visible rect in sheet coordinates.
     */
    calculateViewRect(wholeEl) {
        return calculateViewRectShared(wholeEl, this.findScrollableAncestor.bind(this))
    }

    /**
     * Scrolls the nearest scrollable container for the supplied element.
     * @param {number} x Horizontal scroll delta in pixels.
     * @param {number} y Vertical scroll delta in pixels.
     * @param {HTMLElement} el Element whose scrollable ancestor should be moved.
     * @returns {void}
     */
    scrollParentBy(x, y, el) {
        let parent = this.findScrollableAncestor(el) || document.documentElement
        parent.scrollBy({left: x, top: y, behavior: "auto"})
    }

    /**
     * Wires up intersection and resize observers used by 2D virtualisation.
     * @param {any} dotNetHelper Blazor JS interop helper.
     * @param {HTMLElement} wholeEl Datasheet root element.
     * @param {string} dotnetScrollHandlerName .NET method name used for viewport callbacks.
     * @param {HTMLElement} fillerLeft Left virtualisation filler.
     * @param {HTMLElement} fillerTop Top virtualisation filler.
     * @param {HTMLElement} fillerRight Right virtualisation filler.
     * @param {HTMLElement} fillerBottom Bottom virtualisation filler.
     * @returns {void}
     */
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

    /**
     * Creates a resize observer that re-registers the filler element with the intersection observer.
     * @param {HTMLElement} filler Filler element being watched for size changes.
     * @param {IntersectionObserver} interactionObserver Intersection observer used for virtualisation updates.
     * @returns {ResizeObserver} Resize observer instance.
     */
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


/**
 * Creates a new 2D virtualiser instance.
 * @returns {Virtualiser2d} Virtualiser instance.
 */
export function getVirtualiser() {
    return new Virtualiser2d()
}
