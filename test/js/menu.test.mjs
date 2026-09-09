import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const source = await readFile(new URL('../../src/BlazorDatasheet/wwwroot/js/menu.js', import.meta.url), 'utf8');
const { getMenuService } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

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
