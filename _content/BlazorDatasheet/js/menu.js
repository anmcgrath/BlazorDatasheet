class MenuService {

    constructor(dotnetHelper) {
        this.menus = [];
        // Each open menu, with what had focus when it was requested.
        this.openMenus = new Map()
        this.dotnetHelper = dotnetHelper
        this.disposed = false
        // Each menu waiting to be shown, with its timer.
        this.pendingShows = new Map()
        this.windowMouseDownHandler = this.handleWindowMouseDown.bind(this)
        window.addEventListener('mousedown', this.windowMouseDownHandler)
    }

    handleWindowMouseDown(event) {
        if (this.disposed) return
        let insideMenu = event.target.closest('.bds-sheet-menu') != null
        if (insideMenu)
            return

        for (const menuEl of [...this.openMenus.keys()])
            this.closeMenu(menuEl.id)
    }

    registerMenu(id, parentId) {
        if (this.disposed) return
        this.menus.push({id, parentId});
    }

    unregisterMenu(id) {
        if (this.disposed) return
        if (this.menus.length > 0) {
            let index = this.menus.findIndex(x => x.id === id)
            if (index >= 0)
                this.menus.splice(index, 1)
        }
        // The browser closes a popover removed from the document, so the menu only has to be forgotten.
        for (const [menuEl, timer] of [...this.pendingShows])
            if (menuEl.id === id) {
                clearTimeout(timer)
                this.pendingShows.delete(menuEl)
            }
        for (const menuEl of [...this.openMenus.keys()])
            if (menuEl.id === id) this.openMenus.delete(menuEl)
    }

    showMenu(menuId, options) {
        if (this.disposed) return
        this.menus.forEach(menu => {
            if (menu.id === menuId) {
                let el = document.getElementById(menuId);
                if (el)
                    this.showMenuEl(el, options);
            }
        });
    }

    closeMenu(menuId, closeParent) {
        if (this.disposed) return
        let el = document.getElementById(menuId)
        if (el)
            this.hideMenuEl(el)

        let children = this.getChildren(menuId)
        children.forEach(child => this.closeMenu(child.id))

        if (closeParent) {
            let parent = this.menus.find(menu => menu.id === menuId)
            if (parent) {
                this.closeMenu(parent.parentId, true)
            }
        }
    }

    closeSubMenus(menuId, exceptions) {
        let children = this.getChildren(menuId)
        children.forEach(child => {
                if (exceptions.indexOf(child.id) !== -1)
                    return
                this.closeMenu(child.id)
            }
        )
    }

    getChildren(menuId) {
        return this.menus.filter(menu => menu.parentId === menuId)
    }

    showMenuEl(menuEl, options) {
        if (this.openMenus.has(menuEl) || this.pendingShows.has(menuEl))
            return

        // Whatever had focus when the menu was requested gets it back when the menu closes,
        // unless the user has already moved focus somewhere else in the meantime.
        const opener = document.activeElement

        // run with set timeout to allow the updated menu to be structured based on context
        const timer = setTimeout(() => {
            this.pendingShows.delete(menuEl)
            if (this.disposed || !menuEl.isConnected) return
            if (!menuEl.matches(':popover-open'))
                menuEl.showPopover()
            this.openMenus.set(menuEl, opener)
            if (options.trigger === 'oncontextmenu') {
                let rect = new DOMRect(options.clientX, options.clientY, 1, 1)
                this.positionMenu(menuEl, rect, options.margin, options.placement)
            } else if (options.targetId) {
                let targetEl = document.getElementById(options.targetId)
                if (!targetEl)
                    return
                let targetRect = targetEl.getBoundingClientRect()
                this.positionMenu(menuEl, targetRect, options.margin, options.placement)
            }
        }, 1)
        this.pendingShows.set(menuEl, timer)
    }

    // The close is recorded here rather than on the popover's toggle event, which comes a task later:
    // the mousedown of a right click closes the open menu, and its contextmenu must find it closed.
    hideMenuEl(menuEl) {
        const timer = this.pendingShows.get(menuEl)
        const wasPending = timer !== undefined
        if (wasPending) {
            clearTimeout(timer)
            this.pendingShows.delete(menuEl)
        }

        if (menuEl.matches(':popover-open'))
            menuEl.hidePopover()

        const wasOpen = this.openMenus.has(menuEl)
        if (!wasOpen && !wasPending)
            return

        if (wasOpen) {
            this.restoreFocus(menuEl, this.openMenus.get(menuEl))
            this.openMenus.delete(menuEl)
        }
        this.dotnetHelper.invokeMethodAsync("OnMenuClose", menuEl.id)
    }

    dispose() {
        if (this.disposed) return
        this.disposed = true
        window.removeEventListener('mousedown', this.windowMouseDownHandler)
        for (const timer of this.pendingShows.values()) clearTimeout(timer)
        this.pendingShows.clear()
        this.openMenus.clear()
        this.menus = []
        this.dotnetHelper = null
    }

    restoreFocus(menuEl, opener) {
        const active = document.activeElement
        const focusIsOrphaned = !active || active === document.body || menuEl.contains(active)
        if (focusIsOrphaned && opener?.isConnected && opener !== document.body && opener !== menuEl)
            opener.focus({preventScroll: true})
    }

    positionMenu(menuEl, targetRect, margin, placement, flipCount = 0) {
        let menuRect = menuEl.getBoundingClientRect()
        let x = targetRect.left + targetRect.width / 2 - menuRect.width / 2
        let y = targetRect.top + targetRect.height / 2 - menuRect.height / 2

        if (placement.includes("bottom"))
            y = targetRect.bottom + margin
        else if (placement.includes("top"))
            y = targetRect.top - menuRect.height - margin

        if (placement.includes("right"))
            x = targetRect.right + margin
        else if (placement.includes("left"))
            x = targetRect.left - menuRect.width - margin

        if (x < 0)
            x = margin
        if (y < 0)
            y = margin
        if (x > window.innerWidth - menuRect.width)
            x = window.innerWidth - menuRect.width - margin
        if (y > window.innerHeight - menuRect.height)
            y = window.innerHeight - menuRect.height - margin

        // if the new menu position intersects the target rect, flip the menu
        let newTargetRect = {left: x, top: y, width: menuRect.width, height: menuRect.height}
        if (this.intersects(targetRect, newTargetRect)) {
            // flip x first, then y
            if (flipCount === 0) {
                this.positionMenu(menuEl, targetRect, margin, this.flipPlacementX(placement), flipCount + 1)
                return
            } else if (flipCount === 1) {
                this.positionMenu(menuEl, targetRect, margin, this.flipPlacement(placement), flipCount + 1)
                return
            }
        }

        menuEl.style.top = y + "px"
        menuEl.style.left = x + "px"
    }

    flipPlacementX(placement) {
        if (placement.includes("right"))
            return placement.replace("right", "left")
        if (placement.includes("left"))
            return placement.replace("left", "right")
        return placement
    }

    flipPlacement(placement) {
        if (placement.includes("top"))
            return placement.replace("top", "bottom")
        if (placement.includes("bottom"))
            return placement.replace("bottom", "top")
        if (placement.includes("left"))
            return placement.replace("left", "right")
        if (placement.includes("right"))
            return placement.replace("right", "left")
        return placement
    }

    intersects(rect1, rect2) {
        return rect1.left < rect2.left + rect2.width &&
            rect1.left + rect1.width > rect2.left &&
            rect1.top < rect2.top + rect2.height &&
            rect1.top + rect1.height > rect2.top
    }

}

export function getMenuService(dotnetHelper) {
    return new MenuService(dotnetHelper)
}
