class MenuService {

    constructor() {
        this.menus = [];
        this.activeMenuEls = []

        window.addEventListener('mousedown', this.handleWindowMouseDown.bind(this))
    }

    handleWindowMouseDown(event) {
        let insideMenu = event.target.closest('.sheetMenu') != null
        if (insideMenu)
            return

        this.activeMenuEls.forEach(menuEl => this.closeMenu(menuEl.id))
    }

    registerMenu(id, parentId) {
        this.menus.push({id, parentId});
    }

    showMenu(menuId, options) {
        this.menus.forEach(menu => {
            if (menu.id === menuId) {
                let el = document.getElementById(menuId);
                if (el) {
                    this.activeMenuEl = el
                    this.showMenuEl(el, options);
                }
            }
        });
    }

    closeMenu(menuId, closeParent) {
        let el = document.getElementById(menuId)
        if (el)
            el.hidePopover()

        let children = this.getChildren(menuId)
        children.forEach(child => this.closeMenu(child.id))

        if (closeParent) {
            let parent = this.menus.find(menu => menu.id === menuId)
            if (parent) {
                this.closeMenu(parent.parentId, true)
            }
        }
    }

    closeSubMenus(menuId) {
        let children = this.getChildren(menuId)
        children.forEach(child => this.closeMenu(child.id))
    }

    getChildren(menuId) {
        return this.menus.filter(menu => menu.parentId === menuId)
    }

    isActive(menuEl) {
        return this.activeMenuEls.some(el => el.id === menuEl.id)
    }

    showMenuEl(menuEl, options) {
        if (this.isActive(menuEl))
            return

        // run with set timeout to allow the updated menu to be structured based on context
        setTimeout(() => {
            menuEl.showPopover()
            let self = this

            let onToggle = function (event) {
                if (!self.menus.some(menu => menu.id === event.target.id)) // if menu doesn't exist
                    return
                if (event.newState === 'open') {
                    self.activeMenuEls.push(event.target)
                } else {
                    self.activeMenuEls.splice(self.activeMenuEls.indexOf(event.target), 1)
                    event.target.removeEventListener('toggle', onToggle)
                }
            }

            menuEl.addEventListener('toggle', onToggle)
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

let menuService = null

export function getMenuService() {
    if (menuService == null)
        menuService = new MenuService()
    return menuService
}