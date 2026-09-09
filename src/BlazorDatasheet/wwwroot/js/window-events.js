class WindowEvents {
    constructor(dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.handlerMap = {}
        this.listeners = new Map();
        this.disposed = false;
        this.focused = false;
        this.active = false;
        this.policyRevision = -1;
        this.focusVersion = 0;
        this.preventDefaultMap = {}
    }

    listen(target, name, fn, capture = false) {
        const key = name + ':' + capture;
        const previous = this.listeners.get(key);
        if (previous) previous.target.removeEventListener(name, previous.fn, capture);
        target.addEventListener(name, fn, capture);
        this.listeners.set(key, { target, fn, name, capture });
    }

    registerEvent(eventName, handlerName, throttleInMs = 0) {
        if (this.disposed) return;
        this.handlerMap[eventName] = handlerName;
        const handler = this.handleWindowEvent.bind(this);
        this.listen(window, eventName, throttleInMs ? this.throttle(handler, throttleInMs) : handler);
    }

    dispatch(handler, value) {
        if (this.disposed) return;
        // Invoke in browser event order, but do not wait for earlier callbacks: waiting delays
        // buffered keys past native editor input and can overwrite newer characters.
        return this.dotnetHelper.invokeMethodAsync(handler, value).catch(error => {
            if (!this.disposed) console.error('Datasheet event failed', error);
        });
    }

    configureFocus(container, handler) {
        if (this.disposed) return;
        this.container = container;
        this.focusHandler = handler;
        this.listen(window, 'pointerdown', e => {
            const inside = this.contains(e.target);
            const control = this.controlOf(e.target);
            if (inside && (!control || control === container)) {
                // Chrome treats this scripted focus as keyboard focus, so mark it as pointer
                // driven and let the css drop the focus ring for it.
                container.dataset.pointerFocus = '';
                container.focus({ preventScroll: true });
            }
            if (!inside && !this.inMenu(e.target)) this.setActive(false);
            this.reconcileFocus();
            if (inside && this.focused) this.setFocused(true, true);
        }, true);
        this.listen(window, 'mousedown', e => {
            // Menus render outside the sheet, so clicking an item would hand focus to body and
            // deactivate the sheet. Items act on mouseup, so keeping focus where it is costs nothing.
            if (this.focused && this.inMenu(e.target) && !this.controlOf(e.target)) e.preventDefault();
        }, true);
        this.listen(window, 'focusin', e => {
            this.reconcileFocus();
        }, true);
        this.listen(window, 'focusout', e => {
            if (e.relatedTarget) this.setFocused(this.contains(e.relatedTarget));
            else queueMicrotask(() => {
                // Removing an editor can move focus to body without an external focus destination.
                if (this.focused && e.target.closest?.('.bds-editor-overlay') && !e.target.isConnected)
                    this.restoreFocus();
                this.reconcileFocus();
            });
        }, true);
        this.listen(window, 'blur', () => this.setFocused(false, false, true));
        this.listen(window, 'focus', () => this.reconcileFocus(true));
        this.listen(document, 'visibilitychange', () => this.reconcileFocus(true));
        this.reconcileFocus();
    }

    controlOf(target) {
        return target?.closest?.('input, textarea, select, button, a[href], [contenteditable], [tabindex]');
    }

    inMenu(target) {
        return !!target?.closest?.('.bds-sheet-popover');
    }

    contains(target) {
        return !!target && this.container.contains(target) && target.closest?.('.bds-sheet') === this.container;
    }

    reconcileFocus(fromWindow = false) {
        if (!this.disposed)
            this.setFocused(document.visibilityState !== 'hidden' && document.hasFocus() && this.contains(document.activeElement),
                false, fromWindow);
    }

    // fromWindow marks a change caused by the window or tab losing or regaining focus, as opposed
    // to focus moving between elements on the page. .NET treats the two differently while editing.
    setFocused(focused, activate = false, fromWindow = false) {
        if (this.disposed || (focused === this.focused && !(activate && focused && !this.active))) return;
        this.focused = focused;
        this.active = focused;
        if (this.container) {
            // Focus state is painted from here so the selection dims the instant focus leaves,
            // without waiting on a render round trip.
            if (focused) this.container.dataset.focused = '';
            else {
                delete this.container.dataset.focused;
                delete this.container.dataset.pointerFocus;
            }
        }
        // Browser ownership changes immediately, before any server round trip.
        this.preventDefaultMap.keydown = focused;
        this.dispatchFocus(fromWindow);
    }

    // Deactivation without a focus change, e.g. a pointerdown outside the sheet on something that
    // keeps browser focus where it is. .NET needs to hear about it or the two sides diverge.
    setActive(active) {
        if (this.disposed || active === this.active) return;
        this.active = active;
        this.preventDefaultMap.keydown = active;
        this.dispatchFocus();
    }

    dispatchFocus(fromWindow = false) {
        this.dispatch(this.focusHandler,
            { focused: this.focused, active: this.active, fromWindow, version: ++this.focusVersion });
    }

    setInputState(active, editing, revision, focusVersion) {
        if (this.disposed || revision < this.policyRevision || focusVersion !== this.focusVersion) return;
        this.policyRevision = revision;
        this.active = active;
        this.preventDefaultMap.keydown = active && !editing;
    }

    restoreFocus() {
        if (!this.disposed && this.focused && document.hasFocus() &&
            (this.contains(document.activeElement) || document.activeElement === document.body))
            this.container.focus({ preventScroll: true });
    }

    ownsInput(e) {
        if (!this.container) return true;
        if (document.visibilityState === 'hidden' || !document.hasFocus()) return false;
        if (this.contains(e.target)) {
            if (!this.active) return false;
            // Embedded controls own their input; cell editors still use sheet shortcuts.
            const control = e.target.closest?.('input, textarea, select, button, a[href], [contenteditable]');
            return !control || !!control.closest('.bds-editor-overlay');
        }
        return this.active && (e.target === document.body || e.target === document.documentElement);
    }

    /**
     *
     * @param e {KeyboardEvent}
     */
    async handleWindowEvent(e) {
        if (this.disposed || e.isComposing)
            return

        if (['keydown', 'copy', 'paste'].includes(e.type) && !this.ownsInput(e)) return;

        if (this.handlerMap[e.type]) {
            if (this.container && e.type === 'keydown' && e.key === 'Tab' &&
                e.target.closest?.('.bds-editor-overlay'))
                e.preventDefault();

            // Never suppress the browser default while the event is going into an editable element -
            // that's the cell editor receiving input. The preventDefault flag is toggled from .NET, so
            // it always lags the real edit state by an interop round trip; suppressing on the stale flag
            // swallows the characters typed immediately after an edit begins.
            if (this.preventDefaultMap[e.type] && !this.isEditableTarget(e)) {

                let preventDefault = true

                if (e.type === 'keydown' && e.code === 'KeyV')
                    preventDefault = false

                if (preventDefault)
                    e.preventDefault()
            }

            // Nothing can be prevented past this point - the event has finished dispatching by the time
            // the interop call resolves - so the handler's return value is only used by .NET.
            this.dispatch(this.handlerMap[e.type], this.serialize(e));
        }
    }

    /**
     * Whether the event is targeting something that natively accepts text input.
     * @param e {Event}
     */
    isEditableTarget(e) {
        let target = e.target
        if (!target)
            return false

        if (target.isContentEditable)
            return true

        let tag = target.tagName
        return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT'
    }

    async dispose() {
        this.disposed = true;
        for (const { target, name, fn, capture } of this.listeners.values())
            target.removeEventListener(name, fn, capture);
        this.listeners.clear();
        this.handlerMap = {};
        this.preventDefaultMap = {};
    }

    serialize(e) {
        if (!e)
            return

        if (e.type.includes('key'))
            return this.serializeKeyboardEvent(e)
        else if (e.type.includes('mouse'))
            return this.serializeMouseEvent(e)
        else if (e.type.includes('paste') || e.type.includes('copy'))
            return this.serializeClipboardEvent(e)
    }

    serializeKeyboardEvent(e) {
        if (e) {
            return {
                key: e.key,
                code: e.code,
                location: e.location,
                repeat: e.repeat,
                ctrlKey: e.ctrlKey,
                shiftKey: e.shiftKey,
                altKey: e.altKey,
                metaKey: e.metaKey,
                type: e.type,
                isComposing: e.isComposing,
                isAltGraph: e.getModifierState('AltGraph'),
                // Lets .NET know whether the browser will insert this character itself. If it won't
                // (the editor input isn't focused yet) the character has to be buffered server side.
                isEditableTarget: this.isEditableTarget(e)
            };
        }
    }

    serializeMouseEvent(e) {
        if (e) {
            return {
                type: e.type,
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
                pageY: e.pageY,
                screenX: e.screenX,
                screenY: e.screenY,
            }
        }
    }

    serializeClipboardEvent(e) {
        if (e) {
            if (e.clipboardData && e.clipboardData.getData) {
                let pasteText = ""
                try {
                    pasteText = e.clipboardData.getData('text/plain')
                } catch (ex) {
                    pasteText = ""
                }
                return {
                    text: pasteText,
                    type: e.type
                }
            }
        }
        return {
            text: "",
            type: e.type
        }
    }

// https://stackoverflow.com/questions/27078285/simple-throttle-in-javascript
// Returns a function, that, when invoked, will only be triggered at most once
// during a given window of time. Normally, the throttled function will run
// as much as it can, without ever going more than once per `wait` duration;
// but if you'd like to disable the execution on the leading edge, pass
// `{leading: false}`. To disable execution on the trailing edge, ditto.
    throttle(mainFunction, delay) {
        let timerFlag = null;
        return (...args) => {
            if (timerFlag === null) {
                mainFunction(...args);
                timerFlag = setTimeout(() => {
                    timerFlag = null;
                }, delay);
            }
        };
    }


}

export function createWindowEventsService(dotnetHelper) {
    return new WindowEvents(dotnetHelper);
}
