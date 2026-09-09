using System;
using System.Threading;
using System.Threading.Tasks;
using BlazorDatasheet.Services;
using FluentAssertions;
using Microsoft.JSInterop;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class WindowEventServiceTests
{
    [Test]
    public async Task Disposal_During_Import_Disposes_Module_Without_Registering_Listeners()
    {
        var runtime = new DelayedRuntime();
        var service = new WindowEventService(runtime);
        var calls = 0;
        var initialization = service.ConfigureFocus(default, _ => { calls++; return Task.CompletedTask; });
        await service.DisposeAsync();
        var module = new Module();
        runtime.Completion.SetResult(module);
        await initialization;
        await service.HandleFocusChanged(new SheetFocusEventArgs { Focused = true, Version = 1 });
        module.Disposed.Should().BeTrue();
        module.Invocations.Should().Be(0);
        calls.Should().Be(0);
    }

    [Test]
    public async Task Registering_An_Event_Again_Replaces_The_Previous_Handler()
    {
        using var context = new Bunit.TestContext();
        context.JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        await using var service = new WindowEventService(context.JSInterop.JSRuntime);
        var oldCalls = 0;
        var newCalls = 0;
        await service.RegisterKeyEvent("keydown", _ => { oldCalls++; return Task.FromResult(false); });
        await service.RegisterKeyEvent("keydown", _ => { newCalls++; return Task.FromResult(false); });
        await service.HandleWindowKeyEvent(new SheetKeyboardEventArgs { Type = "keydown" });
        oldCalls.Should().Be(0);
        newCalls.Should().Be(1);
    }

    private sealed class DelayedRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Completion { get; } = new();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            (TValue)await Completion.Task;
    }

    private sealed class Module : IJSObjectReference
    {
        public bool Disposed { get; private set; }
        public int Invocations { get; private set; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Invocations++;
            throw new InvalidOperationException("A disposed service must not invoke its module.");
        }
    }
}
