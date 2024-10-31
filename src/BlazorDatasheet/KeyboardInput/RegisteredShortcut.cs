namespace BlazorDatasheet.KeyboardInput;

internal class RegisteredShortcut
{
    public Func<ShortcutExecutionContext, bool>? Execute { get; }
    public Func<ShortcutExecutionContext, Task<bool>>? ExecuteAsync { get; }
    public Predicate<ShortcutExecutionContext>? CanExecute { get; }

    public RegisteredShortcut(Func<ShortcutExecutionContext, bool> execute,
        Predicate<ShortcutExecutionContext>? canExecute = null)
    {
        Execute = execute;
        CanExecute = canExecute;
    }

    public RegisteredShortcut(Func<ShortcutExecutionContext, Task<bool>> executeAsync,
        Predicate<ShortcutExecutionContext>? canExecute)
    {
        ExecuteAsync = executeAsync;
        CanExecute = canExecute;
    }
}