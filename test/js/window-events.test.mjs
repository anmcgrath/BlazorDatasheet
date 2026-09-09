import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const source = await readFile(new URL('../../src/BlazorDatasheet/wwwroot/js/window-events.js', import.meta.url), 'utf8');
const { createWindowEventsService } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

class Surface extends EventTarget {
    constructor() { super(); this.listeners = new Set(); }
    addEventListener(name, fn, capture) { super.addEventListener(name, fn, capture); this.listeners.add(fn); }
    removeEventListener(name, fn, capture) { super.removeEventListener(name, fn, capture); this.listeners.delete(fn); }
}
function setup() {
    globalThis.window = new Surface();
    globalThis.document = new Surface();
    Object.assign(document, { visibilityState: 'visible', hasFocus: () => true, body: {}, documentElement: {} });
    const container = { dataset: {}, contains: t => t === container || t?.sheet === container, closest: () => container };
    document.activeElement = container;
    const calls = [];
    const service = createWindowEventsService({ invokeMethodAsync: async (...args) => calls.push(args) });
    service.configureFocus(container, 'focus');
    service.registerEvent('keydown', 'key');
    return { service, container, calls };
}
function key(target) {
    return { type: 'keydown', key: 'a', code: 'KeyA', target, getModifierState: () => false,
        preventDefault() { this.prevented = true; } };
}

test('replacement and disposal remove the exact listeners, including throttled handlers', async () => {
    const { service } = setup();
    const count = window.listeners.size;
    service.registerEvent('keydown', 'replacement', 10);
    assert.equal(window.listeners.size, count);
    await service.dispose();
    assert.equal(window.listeners.size, 0);
    assert.equal(document.listeners.size, 0);
});

test('focus ownership suppresses the first key synchronously and delivers focus before keys', async () => {
    const { service, container, calls } = setup();
    const e = key(container);
    await service.handleWindowEvent(e);
    assert.equal(e.prevented, true);
    assert.deepEqual(calls.map(c => c[0]), ['focus', 'key']);
});

test('external inputs, another sheet, and embedded controls keep native keys and clipboard', async () => {
    const { service, container, calls } = setup();
    const external = { tagName: 'INPUT', closest: () => null };
    const other = { closest: () => other };
    const control = { sheet: container, tagName: 'INPUT', closest: selector => selector === '.bds-sheet' ? container : selector === '.bds-editor-overlay' ? null : control };
    service.registerEvent('paste', 'paste');
    for (const target of [external, other, control]) {
        const e = key(target);
        await service.handleWindowEvent(e);
        assert.equal(e.prevented, undefined);
        await service.handleWindowEvent({ type: 'paste', target });
    }
    assert.deepEqual(calls.map(c => c[0]), ['focus']);
});

test('editable cell editor receives native input while sheet shortcuts are forwarded', async () => {
    const { service, container, calls } = setup();
    const editor = { sheet: container, tagName: 'INPUT', closest: selector => selector === '.bds-sheet' ? container : editor };
    const e = key(editor);
    await service.handleWindowEvent(e);
    assert.equal(e.prevented, undefined);
    assert.equal(calls.at(-1)[1].isEditableTarget, true);
});

test('stale server state cannot reactivate after browser focus leaves', async () => {
    const { service } = setup();
    service.setFocused(false);
    service.setInputState(true, false, 10, 1);
    assert.equal(service.active, false);
    assert.equal(service.preventDefaultMap.keydown, false);
    service.setInputState(true, false, 11, 2);
    assert.equal(service.active, true); // explicit activation at the current browser version
    service.setInputState(false, false, 9, 2);
    assert.equal(service.active, true);
});

test('duplicate focus and deferred reconciliation do not emit spurious transitions', async () => {
    const { service, calls } = setup();
    service.reconcileFocus();
    service.reconcileFocus();
    assert.equal(calls.length, 1);
    document.visibilityState = 'hidden';
    service.reconcileFocus();
    service.reconcileFocus();
    assert.equal(calls.length, 2);
    assert.equal(calls[1][1].focused, false);
});

test('late callbacks are dropped on disposal', async () => {
    const { service, container, calls } = setup();
    calls.length = 0;
    await service.dispose();
    service.handleWindowEvent(key(container));
    assert.equal(calls.length, 0);
});

test('clicking a manually deactivated focused sheet reactivates it', () => {
    const { service, calls } = setup();
    service.setInputState(false, false, 1, 1);
    service.setFocused(true, true);
    assert.equal(service.active, true);
    assert.equal(calls.at(-1)[1].version, 2);
});

test('pointer focus is flagged so the focus ring stays a keyboard-only affordance', () => {
    const { service, container } = setup();
    container.focus = () => {};
    service.listeners.get('pointerdown:true').fn({ target: container });
    assert.equal('pointerFocus' in container.dataset, true);
    service.setFocused(false);
    assert.equal('pointerFocus' in container.dataset, false);
});

test('the container is marked focused synchronously so the selection dims without a round trip', () => {
    const { service, container } = setup();
    assert.equal('focused' in container.dataset, true);
    service.setFocused(false);
    assert.equal('focused' in container.dataset, false);
    service.setFocused(true);
    assert.equal('focused' in container.dataset, true);
});

test('a pointerdown outside that keeps browser focus still reports deactivation', async () => {
    const { service, container, calls } = setup();
    const toolbarButton = { closest: () => null };
    service.listeners.get('pointerdown:true').fn({ target: toolbarButton });
    const last = calls.at(-1)[1];
    assert.deepEqual({ focused: last.focused, active: last.active }, { focused: true, active: false });
    assert.equal('focused' in container.dataset, true);
    const e = key(container);
    await service.handleWindowEvent(e);
    assert.equal(e.prevented, undefined);
    assert.equal(calls.at(-1)[0], 'focus');
    container.focus = () => {};
    service.listeners.get('pointerdown:true').fn({ target: container });
    assert.equal(calls.at(-1)[1].active, true);
});

test('clicking a menu item keeps the sheet focused and active', async () => {
    const { service, container, calls } = setup();
    const item = { closest: selector => selector === '.bds-sheet-popover' ? {} : null };
    service.listeners.get('pointerdown:true').fn({ target: item });
    const mousedown = { target: item, preventDefault() { this.prevented = true; } };
    service.listeners.get('mousedown:true').fn(mousedown);
    assert.equal(mousedown.prevented, true);
    assert.deepEqual(calls.map(c => c[0]), ['focus']);
    assert.equal(service.active, true);
    const input = { tagName: 'INPUT', closest: selector => selector === '.bds-sheet-popover' ? {} : selector.includes('input') ? input : null };
    const inputDown = { target: input, preventDefault() { this.prevented = true; } };
    service.listeners.get('mousedown:true').fn(inputDown);
    assert.equal(inputDown.prevented, undefined);
});

test('window focus changes are distinguished from focus moving between elements', () => {
    const { service, container, calls } = setup();
    service.listeners.get('blur:false').fn({});
    assert.deepEqual([calls.at(-1)[1].focused, calls.at(-1)[1].fromWindow], [false, true]);
    service.listeners.get('focus:false').fn({});
    assert.deepEqual([calls.at(-1)[1].focused, calls.at(-1)[1].fromWindow], [true, true]);
    service.listeners.get('focusout:true').fn({ target: container, relatedTarget: { closest: () => null } });
    assert.deepEqual([calls.at(-1)[1].focused, calls.at(-1)[1].fromWindow], [false, false]);
});

test('focus restoration never steals focus from an external control', () => {
    const { service, container } = setup();
    let focused = 0;
    container.focus = () => focused++;
    document.activeElement = { tagName: 'INPUT' };
    service.restoreFocus();
    assert.equal(focused, 0);
    document.activeElement = document.body;
    service.restoreFocus();
    assert.equal(focused, 1);
    service.setFocused(false);
    service.restoreFocus();
    assert.equal(focused, 1);
});

test('Tab in an editor stays within sheet navigation instead of moving native focus', async () => {
    const { service, container } = setup();
    const editor = { sheet: container, tagName: 'INPUT', closest: selector => selector === '.bds-sheet' ? container : editor };
    service.setInputState(true, true, 1, 1);
    const e = { ...key(editor), key: 'Tab', code: 'Tab' };
    await service.handleWindowEvent(e);
    assert.equal(e.prevented, true);
});

test('text editor takes initial focus only after text is applied and never steals external focus', async () => {
    const { container } = setup();
    globalThis.ResizeObserver = class { observe() {} disconnect() {} };
    document.createRange = () => ({ selectNodeContents() {}, collapse() {} });
    document.getSelection = () => ({ removeAllRanges() {}, addRange() {} });
    const input = new Surface();
    Object.assign(input, {
        closest: () => container,
        focus() { document.activeElement = input; input.dispatchEvent(new Event('focus')); }
    });
    const highlighterSource = await readFile(new URL('../../src/BlazorDatasheet/wwwroot/js/highlighter.js', import.meta.url), 'utf8');
    const { createHighlighter } = await import(`data:text/javascript;base64,${Buffer.from(highlighterSource).toString('base64')}`);
    const highlighter = createHighlighter({ inputEl: input, highlightResultEl: {}, initialText: '', initialHtml: '',
        dotnetHelper: { invokeMethodAsync: async () => {} } });
    assert.equal(document.activeElement, container);
    highlighter.setInputText('InitialKey');
    highlighter.focusAndMoveCursorToEnd(true);
    assert.equal(input.textContent, 'InitialKey');
    assert.equal(document.activeElement, input);
    const outside = {};
    document.activeElement = outside;
    highlighter.focusAndMoveCursorToEnd(true);
    assert.equal(document.activeElement, outside);
    document.activeElement = document.body;
    highlighter.focusAndMoveCursorToEnd(true);
    assert.equal(document.activeElement, input);
    let rejectPending;
    highlighter.options.dotnetHelper.invokeMethodAsync = () => new Promise((_, reject) => { rejectPending = reject; });
    const pending = highlighter.invoke('HandleInput', 'late');
    highlighter.dispose();
    rejectPending(new Error('The .NET reference was disposed'));
    await pending;
    assert.equal(input.listeners.size, 0);
    document.activeElement = container;
    highlighter.focusAndMoveCursorToEnd(true);
    assert.equal(document.activeElement, container);
});
