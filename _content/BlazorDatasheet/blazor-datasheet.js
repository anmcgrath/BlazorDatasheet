// Deprecated shim. Blazor now auto-loads "_content/BlazorDatasheet/BlazorDatasheet.lib.module.js".
// Kept for backward compatibility with apps that still include this script explicitly.
import("./BlazorDatasheet.lib.module.js")
    .then(module => module.setupGlobals())
    .catch(() => {
        // Ignore: this script is a non-essential compatibility layer.
    })
