using BlazorDatasheet.Core.Data;
using BlazorDatasheet.KeyboardInput;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Input;

public class ShortcutManager
{
    [Test]
    public void Execute_Func_When_Key_Registered()
    {
        var run = false;
        var func = (ShortcutExecutionContext context) =>
        {
            run = true;
            return true;
        };
        KeyboardInput.ShortcutManager.Register(["k"], KeyboardModifiers.Ctrl, func);

        // execute with another key
        KeyboardInput.ShortcutManager.ExecuteAsync("l", KeyboardModifiers.Ctrl,
            new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();

        // execute with another modifier
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.Ctrl | KeyboardModifiers.Alt,
            new ShortcutExecutionContext(null, null));
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.Meta | KeyboardModifiers.Alt,
            new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();

        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.Ctrl,
            new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
    }

    [Test]
    public void Set_Key_Shortcut_Should_Override_Existing()
    {
        var n = 0;
        var func1 = (ShortcutExecutionContext context) =>
        {
            n = 1;
            return true;
        };
        var func2 = (ShortcutExecutionContext context) =>
        {
            n = 2;
            return true;
        };

        KeyboardInput.ShortcutManager.Register(["k"], KeyboardModifiers.Ctrl, func1);
        KeyboardInput.ShortcutManager.Register(["k"], KeyboardModifiers.Ctrl, func2);

        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.Ctrl, new ShortcutExecutionContext(null, null));
        n.Should().Be(2);
    }

    [Test]
    public void Register_Multuple_Keys_Should_Run_Func_On_Each()
    {
        var run = false;
        var func1 = (ShortcutExecutionContext context) =>
        {
            run = true;
            return true;
        };
        KeyboardInput.ShortcutManager.Register(["k", "l"], KeyboardModifiers.None, func1);
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
        run = false;
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
    }

    [Test]
    public void Predicate_True_Should_Run_Func()
    {
        var run = false;
        KeyboardInput.ShortcutManager.Register(["k"], KeyboardModifiers.Ctrl, (c) => run = true, c => true);
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.Ctrl, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
    }

    [Test]
    public void Predicate_False_Should_Not_Run_Func()
    {
        var run = false;
        KeyboardInput.ShortcutManager.Register(["k"], KeyboardModifiers.None, (c) => run = true, c => false);
        KeyboardInput.ShortcutManager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();
    }
}