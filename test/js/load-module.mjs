import { rmSync } from 'node:fs';
import { cp, mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

// The library's .js files are ES modules that import one another by relative path, which a data: URL
// cannot resolve. A copy beside a package.json declaring "type": "module" lets Node load them as served.
const root = await mkdtemp(join(tmpdir(), 'bds-js-'));
await cp(new URL('../../src/BlazorDatasheet/wwwroot/js/', import.meta.url), root, { recursive: true });
await writeFile(join(root, 'package.json'), '{"type":"module"}');
process.on('exit', () => rmSync(root, { recursive: true, force: true }));

export function importModule(name) {
    return import(pathToFileURL(join(root, name)).href);
}
