import { findScrollableAncestor } from "../../js/scroll-utils.js"
import { watchRemoval } from "../../js/removal-watcher.js"

const accelerationDistance = 120
const maxFrameDuration = 32
const targetSampleInterval = 40

export class AutoScroller {
    subscribe(el, dotnetHelper) {
        this.dotnetHelper = dotnetHelper
        this.sheet = el.closest('.bds-sheet')
        this.ancestor = findScrollableAncestor(el) ?? document.documentElement
        this.onPointerDown = this.onPointerDown.bind(this)
        this.onPointerMove = this.onPointerMove.bind(this)
        this.onPointerUp = this.onPointerUp.bind(this)
        this.step = this.step.bind(this)
        this.sheet?.addEventListener('pointerdown', this.onPointerDown, true)
        this.unwatchRemoval = watchRemoval(el, () => this.dispose())
    }

    configure(active, trackTarget, maxSpeed, edgeThreshold) {
        if (!active)
            this.released = false

        const shouldRun = !!active && !this.released && Number.isFinite(maxSpeed) && maxSpeed > 0
        if (this.trackTarget !== !!trackTarget) {
            this.lastTarget = null
            this.pendingTarget = null
        }
        this.trackTarget = !!trackTarget
        this.maxSpeed = shouldRun ? maxSpeed : 0
        this.edgeThreshold = Number.isFinite(edgeThreshold) ? Math.max(0, edgeThreshold) : 0

        if (shouldRun !== !!this.active) {
            this.active = shouldRun
            if (shouldRun) {
                this.listenDuringDrag()
            } else {
                this.stop()
            }
        }

        if (this.active && this.pointer)
            this.schedule()
    }

    listenDuringDrag() {
        window.addEventListener('pointermove', this.onPointerMove, {passive: true})
        window.addEventListener('pointerup', this.onPointerUp)
        window.addEventListener('pointercancel', this.onPointerUp)
    }

    onPointerDown(e) {
        this.released = false
        this.listenDuringDrag()
        this.onPointerMove(e)
    }

    onPointerMove(e) {
        this.pointer = {
            x: e.clientX, y: e.clientY,
            ctrlKey: e.ctrlKey, shiftKey: e.shiftKey,
            altKey: e.altKey, metaKey: e.metaKey
        }
        this.schedule()
    }

    onPointerUp() {
        this.released = true
        this.active = false
        this.stop()
    }

    schedule() {
        if (this.active && this.pointer && this.frame == null)
            this.frame = requestAnimationFrame(this.step)
    }

    viewportRect() {
        return this.ancestor === document.documentElement
            ? {left: 0, top: 0, right: window.innerWidth, bottom: window.innerHeight}
            : this.ancestor.getBoundingClientRect()
    }

    speed(position, start, end) {
        let distance = 0
        if (position < start + this.edgeThreshold)
            distance = position - start - this.edgeThreshold
        else if (position > end - this.edgeThreshold)
            distance = position - end + this.edgeThreshold

        if (distance === 0)
            return 0
        const fraction = Math.min(Math.max(Math.abs(distance), 12) / accelerationDistance, 1)
        return Math.sign(distance) * this.maxSpeed * Math.pow(fraction, 1.2)
    }

    step(timestamp) {
        this.frame = null
        if (!this.active || !this.pointer)
            return

        const rect = this.viewportRect()
        const vx = this.speed(this.pointer.x, rect.left, rect.right)
        const vy = this.speed(this.pointer.y, rect.top, rect.bottom)
        if (vx === 0 && vy === 0) {
            this.lastFrame = null
            return
        }

        const elapsed = this.lastFrame == null ? 16 : Math.min(timestamp - this.lastFrame, maxFrameDuration)
        this.lastFrame = timestamp
        const beforeX = this.ancestor.scrollLeft
        const beforeY = this.ancestor.scrollTop
        this.ancestor.scrollLeft += vx * elapsed / 1000
        this.ancestor.scrollTop += vy * elapsed / 1000

        if (this.ancestor.scrollLeft !== beforeX || this.ancestor.scrollTop !== beforeY) {
            if (this.trackTarget && (this.lastTargetSample == null || timestamp - this.lastTargetSample >= targetSampleInterval)) {
                this.updateTarget(rect)
                this.lastTargetSample = timestamp
            }
            this.schedule()
        } else {
            if (this.trackTarget)
                this.updateTarget(rect)
            this.lastFrame = null
        }
    }

    updateTarget(rect) {
        if (!this.sheet || !this.pointer)
            return

        const sheetRect = this.sheet.getBoundingClientRect()
        const left = Math.max(rect.left, sheetRect.left)
        const right = Math.min(rect.right, sheetRect.right)
        const top = Math.max(rect.top, sheetRect.top)
        const bottom = Math.min(rect.bottom, sheetRect.bottom)
        if (right - left < 2 || bottom - top < 2)
            return

        const x = Math.max(left + 1, Math.min(this.pointer.x, right - 1))
        const y = Math.max(top + 1, Math.min(this.pointer.y, bottom - 1))
        let cell = document.elementFromPoint(x, y)?.closest?.('.bds-sheet-cell[data-row][data-col]')
        if (!cell || !this.sheet.contains(cell))
            cell = document.elementsFromPoint(x, y)
                .map(element => element.closest?.('.bds-sheet-cell[data-row][data-col]'))
                .find(element => element && this.sheet.contains(element))
        if (!cell)
            return

        const row = Number(cell.dataset.row)
        const col = Number(cell.dataset.col)
        if (!Number.isInteger(row) || !Number.isInteger(col) || (row === -1 && col === -1))
            return

        const key = `${row}:${col}`
        if (key === this.lastTarget)
            return
        this.lastTarget = key
        this.pendingTarget = {
            row, col,
            ctrlKey: this.pointer.ctrlKey, shiftKey: this.pointer.shiftKey,
            altKey: this.pointer.altKey, metaKey: this.pointer.metaKey
        }
        if (!this.sendingTarget)
            void this.flushTarget()
    }

    async flushTarget() {
        this.sendingTarget = true
        try {
            while (this.active && this.trackTarget && this.pendingTarget && this.dotnetHelper) {
                const target = this.pendingTarget
                this.pendingTarget = null
                await this.dotnetHelper.invokeMethodAsync('HandleScrollTarget', target)
            }
        } catch {
            // The component may have been disposed while a callback was in flight.
        } finally {
            this.sendingTarget = false
        }
    }

    stop() {
        window.removeEventListener('pointermove', this.onPointerMove)
        window.removeEventListener('pointerup', this.onPointerUp)
        window.removeEventListener('pointercancel', this.onPointerUp)
        if (this.frame != null)
            cancelAnimationFrame(this.frame)
        this.frame = null
        this.lastFrame = null
        this.pointer = null
        this.lastTarget = null
        this.lastTargetSample = null
        this.pendingTarget = null
    }

    dispose() {
        this.unwatchRemoval?.()
        this.unwatchRemoval = null
        this.active = false
        this.stop()
        this.sheet?.removeEventListener('pointerdown', this.onPointerDown, true)
        this.dotnetHelper = null
        this.sheet = null
    }
}

export function createAutoScroller() {
    return new AutoScroller()
}
