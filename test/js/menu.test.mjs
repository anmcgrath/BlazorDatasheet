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
    first.activeMenuEls = [{ id: 'first' }];
    second.activeMenuEls = [{ id: 'second' }];
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

test('unregistering a menu releases its toggle callback after the element is removed', () => {
    const { service } = setup();
    const menu = new EventTarget();
    menu.id = 'menu';
    let calls = 0;
    const handler = () => { calls++; };
    service.registerMenu(menu.id, null);
    service.toggleHandlers.set(menu, handler);
    menu.addEventListener('toggle', handler);

    service.unregisterMenu(menu.id);
    menu.dispatchEvent(new Event('toggle'));
    assert.equal(calls, 0);
    service.dispose();
});
