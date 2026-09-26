import { test } from 'node:test';
import assert from 'node:assert/strict';
import { importModule } from './load-module.mjs';

const { getInputService } = await importModule('sheet-pointer-input.js');
const { getMenuTargetService } = await importModule('menu-target.js');

test('pointer listeners stop calling .NET after disposal', () => {
    const sheet = new EventTarget();
    sheet.getBoundingClientRect = () => ({x: 0, y: 0});
    sheet.closest = () => ({dataset: {row: '1', col: '2'}});
    const calls = [];
    const service = getInputService(sheet, {
        invokeMethodAsync: name => { calls.push(name); return Promise.resolve(); }
    }, ['up', 'down', 'move', 'enter', 'double']);

    sheet.dispatchEvent(new Event('pointerdown'));
    sheet.dispatchEvent(new Event('pointermove'));
    assert.deepEqual(calls, ['down', 'enter']);

    service.dispose();
    sheet.dispatchEvent(new Event('pointerdown'));
    sheet.dispatchEvent(new Event('pointermove'));
    assert.deepEqual(calls, ['down', 'enter']);
});

test('context menu listener is removed when disabled and on disposal', () => {
    const target = new EventTarget();
    target.tagName = 'DIV';
    target.hasAttribute = () => false;
    const calls = [];
    const service = getMenuTargetService({
        invokeMethodAsync: name => { calls.push(name); return Promise.resolve(); }
    });

    service.setContextListener(target, 'context');
    target.dispatchEvent(new Event('contextmenu', {cancelable: true}));
    service.removeContextListener();
    target.dispatchEvent(new Event('contextmenu', {cancelable: true}));
    service.setContextListener(target, 'context');
    target.dispatchEvent(new Event('contextmenu', {cancelable: true}));
    service.dispose();
    target.dispatchEvent(new Event('contextmenu', {cancelable: true}));
    assert.deepEqual(calls, ['context', 'context']);
});

test('removed DOM roots clean up listeners without a .NET disposal call', () => {
    const previousObserver = globalThis.MutationObserver;
    const previousDocument = globalThis.document;
    const observers = [];
    globalThis.document = {body: {}};
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; observers.push(this); }
        observe() {}
        disconnect() { this.disconnected = true; }
    };

    try {
        const sheet = new EventTarget();
        sheet.isConnected = true;
        sheet.getBoundingClientRect = () => ({x: 0, y: 0});
        sheet.closest = () => ({dataset: {row: '1', col: '2'}});
        let calls = 0;
        getInputService(sheet, {invokeMethodAsync: () => { calls++; return Promise.resolve(); }},
            ['up', 'down', 'move', 'enter', 'double']);
        sheet.isConnected = false;
        observers[0].callback();
        sheet.dispatchEvent(new Event('pointerdown'));
        assert.equal(calls, 0);
        assert.equal(observers[0].disconnected, true);

        const target = new EventTarget();
        target.isConnected = true;
        target.tagName = 'DIV';
        target.hasAttribute = () => false;
        const service = getMenuTargetService({invokeMethodAsync: () => { calls++; return Promise.resolve(); }});
        service.setContextListener(target, 'context');
        target.isConnected = false;
        observers[1].callback();
        target.dispatchEvent(new Event('contextmenu'));
        assert.equal(calls, 0);
        assert.equal(observers[1].disconnected, true);
    } finally {
        globalThis.MutationObserver = previousObserver;
        globalThis.document = previousDocument;
    }
});

test('removal watching shares one document observer and stops it once nothing is watched', async () => {
    const previousObserver = globalThis.MutationObserver;
    const previousDocument = globalThis.document;
    const observers = [];
    globalThis.document = {body: {}};
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; observers.push(this); }
        observe() {}
        disconnect() { this.disconnected = true; }
    };

    try {
        const { watchRemoval } = await importModule('removal-watcher.js');
        const first = {isConnected: true};
        const second = {isConnected: true};
        const removed = [];
        watchRemoval(first, () => removed.push('first'));
        const unwatchSecond = watchRemoval(second, () => removed.push('second'));
        assert.equal(observers.length, 1);

        first.isConnected = false;
        observers[0].callback();
        assert.deepEqual(removed, ['first']);
        assert.equal(observers[0].disconnected, undefined);

        unwatchSecond();
        assert.equal(observers[0].disconnected, true);
        second.isConnected = false;
        observers[0].callback();
        assert.deepEqual(removed, ['first']);
    } finally {
        globalThis.MutationObserver = previousObserver;
        globalThis.document = previousDocument;
    }
});
