Run the browser event regression tests from the repository root:

```sh
node --test test/js/*.test.mjs
```

These use Node's built-in test runner and lightweight DOM event fixtures, with no npm dependencies.
They cover input ownership, focus transitions, stale interop updates, editor focus readiness,
and listener disposal. Real browser validation is also needed for native focus and editor behavior.
