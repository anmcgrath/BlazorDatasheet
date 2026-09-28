import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'

const source = await readFile(new URL('../../src/BlazorDatasheet/Render/AutoScroll/AutoScroller.razor.js', import.meta.url), 'utf8')
const moduleSource = source.replace(/^import .*\n/, 'const findScrollableAncestor = () => globalThis.testAncestor\n')
const { createAutoScroller } = await import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`)

function setup() {
    const frames = new Map()
    let nextFrame = 1
    globalThis.requestAnimationFrame = callback => {
        const id = nextFrame++
        frames.set(id, callback)
        return id
    }
    globalThis.cancelAnimationFrame = id => frames.delete(id)
    globalThis.window = new EventTarget()
    Object.assign(window, {innerWidth: 100, innerHeight: 100})

    const cell = {dataset: {row: '2', col: '3'}}
    const sheet = {
        getBoundingClientRect: () => ({left: 0, top: 0, right: 100, bottom: 100}),
        contains: element => element === cell,
        addEventListener: () => {}, removeEventListener: () => {}
    }
    globalThis.document = {
        documentElement: {},
        elementFromPoint: () => ({closest: () => cell}),
        elementsFromPoint: () => [{closest: () => cell}]
    }
    const ancestor = {
        scrollLeft: 0, scrollTop: 0,
        getBoundingClientRect: () => ({left: 0, top: 0, right: 100, bottom: 100})
    }
    globalThis.testAncestor = ancestor

    const calls = []
    const scroller = createAutoScroller()
    scroller.subscribe({closest: () => sheet}, {
        invokeMethodAsync: async (...args) => calls.push(args)
    })
    const tick = timestamp => {
        const [id, callback] = frames.entries().next().value ?? []
        assert.ok(callback, 'expected an animation frame')
        frames.delete(id)
        callback(timestamp)
    }
    return {scroller, ancestor, calls, frames, tick, cell}
}

test('scrolls at a frame-independent rate and only reports a changed target', async () => {
    const {scroller, ancestor, calls, tick, cell} = setup()
    scroller.configure(true, true, 500, 16)
    assert.equal(scroller.frame, undefined)
    scroller.onPointerMove({clientX: 50, clientY: 110})
    tick(16)
    const first = ancestor.scrollTop
    assert.ok(first > 0 && first < 8)
    await Promise.resolve()
    assert.equal(calls.length, 1)
    assert.equal(calls[0][0], 'HandleScrollTarget')
    assert.equal(calls[0][1].row, 2)

    tick(32)
    assert.ok(Math.abs((ancestor.scrollTop - first) - first) < 0.01)
    await Promise.resolve()
    assert.equal(calls.length, 1)

    cell.dataset.row = '4'
    tick(80)
    await Promise.resolve()
    assert.equal(calls.length, 2)
    scroller.dispose()
})

test('stops immediately inside the viewport, on release, and at a scroll boundary', () => {
    const {scroller, ancestor, frames, tick} = setup()
    scroller.configure(true, false, 500, 16)
    scroller.onPointerMove({clientX: 50, clientY: 110})
    tick(16)
    scroller.onPointerMove({clientX: 50, clientY: 50})
    tick(32)
    assert.equal(frames.size, 0)

    scroller.onPointerMove({clientX: 50, clientY: 110})
    scroller.onPointerUp()
    assert.equal(frames.size, 0)
    scroller.configure(true, false, 500, 16)
    assert.equal(scroller.active, false)
    scroller.configure(false, false, 500, 16)
    scroller.configure(true, false, 500, 16)
    scroller.onPointerMove({clientX: 50, clientY: 110})
    Object.defineProperty(ancestor, 'scrollTop', {get: () => 100, set: () => {}})
    tick(48)
    assert.equal(frames.size, 0)
    scroller.dispose()
})

test('remembers a drag that reaches the edge before activation arrives', () => {
    const {scroller, ancestor, frames, tick} = setup()
    scroller.onPointerDown({clientX: 50, clientY: 50})
    scroller.onPointerMove({clientX: 50, clientY: 110})
    assert.equal(frames.size, 0)

    scroller.configure(true, false, 500, 16)
    tick(16)
    assert.ok(ancestor.scrollTop > 0)
    scroller.dispose()
})

test('coalesces cell changes while a server callback is in flight', async () => {
    const {scroller, cell, calls} = setup()
    const resolvers = []
    scroller.dotnetHelper = {
        invokeMethodAsync: (...args) => {
            calls.push(args)
            return new Promise(resolve => resolvers.push(resolve))
        }
    }
    scroller.configure(true, true, 500, 16)
    scroller.onPointerMove({clientX: 50, clientY: 110})
    const rect = scroller.viewportRect()
    scroller.updateTarget(rect)
    cell.dataset.row = '4'
    scroller.updateTarget(rect)
    cell.dataset.row = '5'
    scroller.updateTarget(rect)
    assert.equal(calls.length, 1)

    resolvers.shift()()
    await Promise.resolve()
    assert.equal(calls.length, 2)
    assert.equal(calls[1][1].row, 5)
    resolvers.shift()()
    await Promise.resolve()
    scroller.dispose()
})
