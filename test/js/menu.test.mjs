import { test } from 'node:test';
import assert from 'node:assert/strict';
import { importModule } from './load-module.mjs';
const { getMenuService } = await importModule('menu.js');

function setup() {
    globalThis.window = new EventTarget();
    globalThis.document = { body: {} };
    const service = getMenuService({ invokeMethodAsync: async () => {} });
    const menu = { id: 'menu', contains: t => t?.menu === menu };
    const opener = { isConnected: true, focus() { this.focused = true; } };
    return { service, menu, opener };
}

test('closing a menu returns focus to the opener when focus was left on body or inside the menu', () => {
    const { service, menu, opener } = setup();
    document.activeElement = document.body;
    service.restoreFocus(menu, opener);
    assert.equal(opener.focused, true);
    opener.focused = false;
    document.activeElement = { menu };
    service.restoreFocus(menu, opener);
    assert.equal(opener.focused, true);
});

test('closing a menu leaves focus alone when the user moved it elsewhere or the opener is gone', () => {
    const { service, menu, opener } = setup();
    document.activeElement = { tagName: 'INPUT' };
    service.restoreFocus(menu, opener);
    assert.equal(opener.focused, undefined);
    document.activeElement = document.body;
    opener.isConnected = false;
    service.restoreFocus(menu, opener);
    assert.equal(opener.focused, undefined);
});

test('menu services are separate and release their window listener on disposal', () => {
    const browser = new EventTarget();
    globalThis.window = browser;
    const first = getMenuService({ invokeMethodAsync: async () => {} });
    const second = getMenuService({ invokeMethodAsync: async () => {} });
    assert.notEqual(first, second);

    let firstCalls = 0;
    let secondCalls = 0;
    first.openMenus.set({ id: 'first' }, null);
    second.openMenus.set({ id: 'second' }, null);
    first.closeMenu = () => { firstCalls++; };
    second.closeMenu = () => { secondCalls++; };
    const event = new Event('mousedown');
    Object.defineProperty(event, 'target', { value: { closest: () => null } });
    browser.dispatchEvent(event);
    assert.equal(firstCalls, 1);
    assert.equal(secondCalls, 1);

    first.dispose();
    browser.dispatchEvent(event);
    assert.equal(firstCalls, 1);
    assert.equal(secondCalls, 2);
    second.dispose();
});

function popover(id) {
    const el = {
        id, isConnected: true, open: false, style: {},
        matches: () => el.open,
        showPopover() { el.open = true; },
        hidePopover() { el.open = false; },
        contains: () => false,
        getBoundingClientRect: () => ({ width: 10, height: 10 }),
    };
    return el;
}

const nextTask = () => new Promise(resolve => setTimeout(resolve, 5));

test('a right click on another cell closes the open menu and opens it again in the same gesture', async () => {
    globalThis.window = Object.assign(new EventTarget(), { innerWidth: 1000, innerHeight: 1000 });
    globalThis.DOMRect = class { constructor(x, y, w, h) { Object.assign(this, { left: x, top: y, width: w, height: h, right: x + w, bottom: y + h }); } };
    const menu = popover('menu');
    globalThis.document = { body: {}, activeElement: null, getElementById: id => id === 'menu' ? menu : null };
    const closed = [];
    const service = getMenuService({ invokeMethodAsync: async (name, id) => { closed.push(id); } });
    service.registerMenu('menu', null);
    const options = { trigger: 'oncontextmenu', clientX: 1, clientY: 1, margin: 0, placement: 'bottom' };

    service.showMenu('menu', options);
    await nextTask();
    assert.equal(menu.open, true);

    const mousedown = new Event('mousedown');
    Object.defineProperty(mousedown, 'target', { value: { closest: () => null } });
    window.dispatchEvent(mousedown);
    assert.deepEqual(closed, ['menu']);

    service.showMenu('menu', options);
    await nextTask();
    assert.equal(menu.open, true);
    service.dispose();
});

test('unregistering a menu forgets it, open or about to open', async () => {
    const { service } = setup();
    const menu = popover('menu');
    document.getElementById = () => menu;
    service.registerMenu(menu.id, null);
    service.showMenu(menu.id, { trigger: 'onclick' });

    service.unregisterMenu(menu.id);
    await nextTask();
    assert.equal(menu.open, false);
    assert.equal(service.openMenus.size, 0);
    service.dispose();
});
