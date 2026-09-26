using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlazorDatasheet.Menu;
using BlazorDatasheet.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class SheetMenuTargetInteropTests
{
    [Test]
    public void Renders_While_The_Service_Is_Created_Attach_One_Listener()
    {
        using var context = new TestContext();
        var runtime = new PendingImportRuntime();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        context.Services.AddSingleton<IMenuService>(new MenuService(runtime));

        var cut = context.RenderComponent<SheetMenuTarget>(p => p
            .Add(x => x.MenuId, "menu")
            .Add(x => x.Trigger, MenuTrigger.OnContextMenu));
        cut.Render();
        cut.Render();
        runtime.Imports.Should().Be(1);

        runtime.CompleteImport();

        cut.WaitForAssertion(() => runtime.Service.Calls.Should().Equal("setContextListener"));
        runtime.Module.Calls.Should().Equal("getMenuTargetService");
    }

    [Test]
    public void Disabling_While_The_Service_Is_Created_Attaches_Nothing()
    {
        using var context = new TestContext();
        var runtime = new PendingImportRuntime();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        context.Services.AddSingleton<IMenuService>(new MenuService(runtime));

        var cut = context.RenderComponent<SheetMenuTarget>(p => p
            .Add(x => x.MenuId, "menu")
            .Add(x => x.Trigger, MenuTrigger.OnContextMenu));
        cut.SetParametersAndRender(p => p.Add(x => x.DisableMenuTarget, true));

        runtime.CompleteImport();

        cut.WaitForAssertion(() => runtime.Module.Calls.Should().Equal("getMenuTargetService"));
        runtime.Service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Toggling_The_Target_Detaches_And_Reattaches_The_Listener()
    {
        using var context = new TestContext();
        var runtime = new PendingImportRuntime();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        context.Services.AddSingleton<IMenuService>(new MenuService(runtime));

        var cut = context.RenderComponent<SheetMenuTarget>(p => p
            .Add(x => x.MenuId, "menu")
            .Add(x => x.Trigger, MenuTrigger.OnContextMenu));
        runtime.CompleteImport();
        cut.WaitForAssertion(() => runtime.Service.Calls.Should().Equal("setContextListener"));

        cut.SetParametersAndRender(p => p.Add(x => x.DisableMenuTarget, true));
        cut.SetParametersAndRender(p => p.Add(x => x.DisableMenuTarget, false));

        cut.WaitForAssertion(() => runtime.Service.Calls.Should()
            .Equal("setContextListener", "removeContextListener", "setContextListener"));
        runtime.Imports.Should().Be(1);
    }

    private sealed class PendingImportRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<object> _import =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Imports;
        public RecordingReference Service { get; } = new(null);
        public RecordingReference Module { get; }

        public PendingImportRuntime() => Module = new RecordingReference(Service);

        public void CompleteImport() => _import.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier != "import" || !(args?.FirstOrDefault() as string ?? "").EndsWith("menu-target.js"))
                return default!;
            Interlocked.Increment(ref Imports);
            return (TValue)await _import.Task;
        }
    }

    private sealed class RecordingReference(RecordingReference? created) : IJSObjectReference
    {
        private readonly ConcurrentQueue<string> _calls = new();

        public string[] Calls => _calls.Where(x => x != "dispose").ToArray();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, default, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args)
        {
            _calls.Enqueue(identifier);
            return ValueTask.FromResult(created is not null ? (TValue)(object)created : default(TValue)!);
        }
    }
}
