class WindowEvents {
    constructor(dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.handlerMap = {}
        this.preventDefaultMap = {}
        this.preventExclusionsMap = {}
    }

    registerEvent(eventName, handlerName, throttleInMs = 0) {
        try {
            if (this.handlerMap[eventName])
                window.removeEventListener(eventName, this.handleWindowEvent)

            this.handlerMap[eventName] = handlerName;
            let fn = throttleInMs === 0 ?
                this.handleWindowEvent.bind(this) : this.throttle(this.handleWindowEvent.bind(this), throttleInMs)
            window.addEventListener(eventName, fn)
        } catch (ex) {
            return false
        }

    }

    preventDefault(eventName, exclusions) {
        this.preventDefaultMap[eventName] = true;
        this.preventExclusionsMap[eventName] = exclusions;

    }

    cancelPreventDefault(eventType) {
        this.preventDefaultMap[eventType] = false;
        this.preventExclusionsMap[eventType] = []
    }

    /**
     *
     * @param e {KeyboardEvent}
     */
    async handleWindowEvent(e) {
        if (e.isComposing)
            return

        if (this.handlerMap[e.type]) {
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
            await this.dotnetHelper.invokeMethodAsync(this.handlerMap[e.type], this.serialize(e));
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
        for (let eventName in this.handlerMap) {
            window.removeEventListener(eventName, this.handleWindowEvent)
        }
        this.handlerMap = {}
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
