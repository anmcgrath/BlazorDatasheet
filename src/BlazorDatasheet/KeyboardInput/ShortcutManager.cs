namespace BlazorDatasheet.KeyboardInput;

public class ShortcutManager
{
    private readonly Dictionary<MappedKey, RegisteredShortcut> _keyMap = new();

    /// <summary>
    /// Register a keyboard event based on the browser's key attribute (the character/key the user is inputting) or
    /// the the browser's code attribute (independent of keyboard layout.
    /// </summary>
    /// <param name="keys">The list of keys/codes that this shortcut should be executed for</param>
    /// <param name="modifiers">The modifiers that the user should be pressing, can be more than one</param>
    /// <param name="execute">The function that should execute when the shortcut is run</param>
    /// <param name="canExecute"></param>
    public void Register(string[] keys, KeyboardModifiers[] modifiers,
        Func<ShortcutExecutionContext, bool> execute,
        Predicate<ShortcutExecutionContext>? canExecute = null)
    {
        var shortcut = new RegisteredShortcut(execute, canExecute);
        foreach (var key in keys)
        {
            foreach (var modifier in modifiers)
            {
                var map = new MappedKey(key, modifier);
                if (!_keyMap.TryAdd(map, shortcut))
                    _keyMap[map] = shortcut;
            }
        }
    }

    /// <summary>
    /// Register a keyboard event based on the browser's key attribute (the character/key the user is inputting) or
    /// the the browser's code attribute (independent of keyboard layout.
    /// </summary>
    /// <param name="keys">The list of keys/codes that this shortcut should be executed for</param>
    /// <param name="modifiers">The modifiers that the user should be pressing, can be more than one</param>
    /// <param name="executeAsync">The function that should execute when the shortcut is run</param>
    /// <param name="canExecute"></param>
    public void Register(string[] keys, KeyboardModifiers[] modifiers,
        Func<ShortcutExecutionContext, Task<bool>> executeAsync,
        Predicate<ShortcutExecutionContext>? canExecute = null)
    {
        var shortcut = new RegisteredShortcut(executeAsync, canExecute);
        foreach (var key in keys)
        {
            foreach (var modifier in modifiers)
            {
                var map = new MappedKey(key, modifier);
                if (!_keyMap.TryAdd(map, shortcut))
                    _keyMap[map] = shortcut;
            }
        }
    }

    /// <summary>
    /// Register a keyboard event based on the browser's key attribute (the character/key the user is inputting) or
    /// the the browser's code attribute (independent of keyboard layout.
    /// </summary>
    /// <param name="keys">The list of keys/codes that this shortcut should be executed for</param>
    /// <param name="modifier">The modifiers that the user should be pressing, can be more than one</param>
    /// <param name="executeAsync">The function that should execute when the shortcut is run</param>
    /// <param name="canExecute"></param>
    public void Register(string[] keys, KeyboardModifiers modifier,
        Func<ShortcutExecutionContext, Task<bool>> executeAsync,
        Predicate<ShortcutExecutionContext>? canExecute = null) => Register(keys, [modifier], executeAsync, canExecute);

    /// <summary>
    /// Register a keyboard event based on the browser's key attribute (the character/key the user is inputting) or
    /// the the browser's code attribute (independent of keyboard layout.
    /// </summary>
    /// <param name="keys">The list of keys/codes that this shortcut should be executed for</param>
    /// <param name="modifier">The modifiers that the user should be pressing, can be more than one</param>
    /// <param name="execute">The function that should execute when the shortcut is run</param>
    /// <param name="canExecute"></param>
    public void Register(string[] keys, KeyboardModifiers modifier,
        Func<ShortcutExecutionContext, bool> execute,
        Predicate<ShortcutExecutionContext>? canExecute = null) => Register(keys, [modifier], execute, canExecute);

    /// <summary>
    /// Run the function associated with the key, and the <paramref name="modifiers"/>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="modifiers"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal async Task<bool> ExecuteAsync(string key, KeyboardModifiers modifiers,
        ShortcutExecutionContext context)
    {
        context.Key = key;
        context.Modifiers = modifiers;

        if (_keyMap.TryGetValue(new MappedKey(key, KeyboardModifiers.Any), out var anyShortcut))
        {
            if (anyShortcut.CanExecute == null || (anyShortcut.CanExecute != null && anyShortcut.CanExecute(context)))
            {
                var handled = false;
                if (anyShortcut.Execute != null)
                    handled = anyShortcut.Execute(context);
                if (anyShortcut.ExecuteAsync != null)
                    handled = await anyShortcut.ExecuteAsync(context);

                if (handled)
                    return true;
            }
        }

        if (_keyMap.TryGetValue(new MappedKey(key, modifiers), out var keyShortcut))
        {
            if (keyShortcut.CanExecute == null || (keyShortcut.CanExecute != null && keyShortcut.CanExecute(context)))
            {
                var handled = false;
                if (keyShortcut.Execute != null)
                    handled = keyShortcut.Execute(context);
                if (keyShortcut.ExecuteAsync != null)
                    handled = await keyShortcut.ExecuteAsync(context);
                if (handled)
                    return true;
            }
        }

        return false;
    }
}