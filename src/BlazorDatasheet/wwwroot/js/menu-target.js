class MenuTargetService {

    constructor(dotnetHelper) {
        this.targetEl = {}
        this.handlerName = null
        this.dotnetHelper = dotnetHelper
    }


    setContextListener(el, handlerName) {
        this.handlerName = handlerName
        this.targetEl = el
        this.targetEl.addEventListener('contextmenu', this.handleContextMenu.bind(this))
    }

    removeContextListener() {
        this.targetEl.removeEventListener('contextmenu', this.handleContextMenu)
    }

    handleContextMenu(e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.hasAttribute("contentEditable"))
            return

        e.preventDefault()
        this.dotnetHelper.invokeMethodAsync(this.handlerName, {clientX: e.clientX, clientY: e.clientY})
    }

}

export function getMenuTargetService(dotnetHelper) {
    return new MenuTargetService(dotnetHelper)
}