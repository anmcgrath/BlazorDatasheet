const watched = new Map() // element -> Set of callbacks
let observer = null

/**
 * Calls onRemoved once the element is no longer connected to the document.
 * @param {Node} el
 * @param {() => void} onRemoved
 * @returns {() => void} Stops watching.
 */
export function watchRemoval(el, onRemoved) {
    if (typeof MutationObserver === 'undefined')
        return () => {}

    let callbacks = watched.get(el)
    if (!callbacks)
        watched.set(el, callbacks = new Set())
    callbacks.add(onRemoved)

    if (!observer) {
        observer = new MutationObserver(check)
        observer.observe(document.body, {childList: true, subtree: true})
    }

    return () => unwatch(el, onRemoved)
}

function unwatch(el, onRemoved) {
    const callbacks = watched.get(el)
    if (!callbacks)
        return
    callbacks.delete(onRemoved)
    if (!callbacks.size)
        watched.delete(el)
    stopIfIdle()
}

function check() {
    for (const [el, callbacks] of watched) {
        if (el.isConnected)
            continue
        watched.delete(el)
        for (const callback of callbacks)
            callback()
    }
    stopIfIdle()
}

function stopIfIdle() {
    if (watched.size || !observer)
        return
    observer.disconnect()
    observer = null
}
