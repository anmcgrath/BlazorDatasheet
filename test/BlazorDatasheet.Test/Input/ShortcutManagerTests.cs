using BlazorDatasheet.Core.Data;
using BlazorDatasheet.KeyboardInput;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Input;

public class ShortcutManagerTests
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
        var manager = new ShortcutManager();
        manager.Register(["k"], KeyboardModifiers.Ctrl, func);

        // execute with another key
        manager.ExecuteAsync("l", KeyboardModifiers.Ctrl,
            new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();

        // execute with another modifier
        manager.ExecuteAsync("k", KeyboardModifiers.Ctrl | KeyboardModifiers.Alt,
            new ShortcutExecutionContext(null, null));
        manager.ExecuteAsync("k", KeyboardModifiers.Meta | KeyboardModifiers.Alt,
            new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();

        manager.ExecuteAsync("k", KeyboardModifiers.Ctrl,
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

        var manager = new ShortcutManager();
        manager.Register(["k"], KeyboardModifiers.Ctrl, func1);
        manager.Register(["k"], KeyboardModifiers.Ctrl, func2);

        manager.ExecuteAsync("k", KeyboardModifiers.Ctrl, new ShortcutExecutionContext(null, null));
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
        var manager = new ShortcutManager();
        manager.Register(["k", "l"], KeyboardModifiers.None, func1);
        manager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
        run = false;
        manager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
    }

    [Test]
    public void Predicate_True_Should_Run_Func()
    {
        var manager = new ShortcutManager();
        var run = false;
        manager.Register(["k"], KeyboardModifiers.Ctrl, (c) => run = true, c => true);
        manager.ExecuteAsync("k", KeyboardModifiers.Ctrl, new ShortcutExecutionContext(null, null));
        run.Should().BeTrue();
    }

    [Test]
    public void Predicate_False_Should_Not_Run_Func()
    {
        var manager = new ShortcutManager();
        var run = false;
        manager.Register(["k"], KeyboardModifiers.None, (c) => run = true, c => false);
        manager.ExecuteAsync("k", KeyboardModifiers.None, new ShortcutExecutionContext(null, null));
        run.Should().BeFalse();
    }
}