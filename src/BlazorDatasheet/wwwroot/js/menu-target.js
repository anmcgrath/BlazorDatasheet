import { watchRemoval } from "./removal-watcher.js"

class MenuTargetService {

    constructor(dotnetHelper) {
        this.targetEl = null
        this.handlerName = null
        this.dotnetHelper = dotnetHelper
        this.contextMenuHandler = this.handleContextMenu.bind(this)
    }


    setContextListener(el, handlerName) {
        this.removeContextListener()
        this.handlerName = handlerName
        this.targetEl = el
        this.targetEl.addEventListener('contextmenu', this.contextMenuHandler)
        this.unwatchRemoval = watchRemoval(el, () => this.dispose())
    }

    removeContextListener() {
        this.unwatchRemoval?.()
        this.unwatchRemoval = null
        this.targetEl?.removeEventListener('contextmenu', this.contextMenuHandler)
        this.targetEl = null
    }

    handleContextMenu(e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.hasAttribute("contentEditable"))
            return

        e.preventDefault()
        this.dotnetHelper.invokeMethodAsync(this.handlerName, {clientX: e.clientX, clientY: e.clientY})
    }
    
    dispose(){
        this.removeContextListener()
        this.dotnetHelper = null
    }

}

export function getMenuTargetService(dotnetHelper) {
    return new MenuTargetService(dotnetHelper)
}
