class WindowEvents {
    constructor(dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.handlerMap = {}
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

    /**
     *
     * @param e {MouseEvent}
     */
    async handleWindowEvent(e) {
        if (this.handlerMap[e.type]) {
            let respIsHandled = await this.dotnetHelper.invokeMethodAsync(this.handlerMap[e.type], this.serialize(e));
            if (respIsHandled === true) {
                e.preventDefault();
                e.stopImmediatePropagation();
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