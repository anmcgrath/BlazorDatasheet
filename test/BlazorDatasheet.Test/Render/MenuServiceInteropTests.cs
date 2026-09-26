using System;
using System.Threading;
using System.Threading.Tasks;
using BlazorDatasheet.Services;
using FluentAssertions;
using Microsoft.JSInterop;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class MenuServiceInteropTests
{
    [Test]
    public async Task Disposal_Calls_Js_Service_Cleanup_Before_Releasing_Reference()
    {
        var menuJs = new MenuJs();
        var module = new MenuModule(menuJs);
        var service = new MenuService(new ModuleRuntime(module));

        await service.CloseMenu("missing");
        await service.DisposeAsync();

        menuJs.CleanupCalled.Should().BeTrue();
        menuJs.Disposed.Should().BeTrue();
        module.Disposed.Should().BeTrue();
    }

    private sealed class ModuleRuntime(MenuModule module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult((TValue)(object)module);
    }

    private sealed class MenuModule(MenuJs menuJs) : IJSObjectReference
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult((TValue)(object)menuJs);
    }

    private sealed class MenuJs : IJSObjectReference
    {
        public bool CleanupCalled { get; private set; }
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "dispose") CleanupCalled = true;
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
