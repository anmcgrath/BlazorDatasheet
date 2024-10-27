class WindowEvents {
    constructor(dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.handlerMap = {}
        this.preventDefaultMap = {}
        this.preventExclusionsMap = {}
    }

    registerEvent(eventName, handlerName) {
        try {
            if (this.handlerMap[eventName])
                window.removeEventListener(eventName, this.handleWindowEvent)

            this.handlerMap[eventName] = handlerName;
            window.addEventListener(eventName, this.handleWindowEvent.bind(this))
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
     * @param e {MouseEvent}
     */
    async handleWindowEvent(e) {
        if (this.handlerMap[e.type]) {
            if (this.preventDefaultMap[e.type]) {

                let preventDefault = true
                let evSerialized = this.serialize(e)

                if (this.preventExclusionsMap[e.type]) {
                    // if one of these exclusions match the keyboard event,
                    // don't prevent default

                    for (const exclusion of this.preventExclusionsMap[e.type]) {
                        let exclusionMatches = true
                        for (const prop in exclusion) {
                            if (exclusion[prop] == null)
                                continue
                            if (exclusion[prop] !== evSerialized[prop]) {
                                exclusionMatches = false
                                break
                            }
                        }
                        if (exclusionMatches) {
                            preventDefault = false
                            break
                        }
                    }
                }

                if (preventDefault)
                    e.preventDefault()
            }

            let respIsHandled = await this.dotnetHelper.invokeMethodAsync(this.handlerMap[e.type], this.serialize(e));
            if (respIsHandled === true) {
                e.preventDefault();
            }
        }
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
        else if (e.type.includes('paste'))
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
                type: e.type
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

}

export function createWindowEvents(dotnetHelper) {
    return new WindowEvents(dotnetHelper);
}