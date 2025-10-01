using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using BlazorFastMarquee;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace BlazorFastMarquee.Tests;

public class MarqueeTests : TestContext
{
    [Fact]
    public void RendersWithDefaultMarkup()
    {
        var jsRuntime = new StubJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cut = RenderComponent<Marquee>(parameters => parameters
            .AddChildContent("Hello world"));

        var container = cut.Find(".bfm-marquee-container");
        Assert.Contains("--pause-on-hover", container.GetAttribute("style"));
        Assert.Equal("Hello world", cut.Find(".bfm-child").TextContent.Trim());
    }

    [Fact]
    public async Task AutoFillUpdatesMultiplierWhenLayoutChanges()
    {
        var jsRuntime = new StubJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cut = RenderComponent<Marquee>(parameters => parameters
            .Add(p => p.AutoFill, true)
            .AddChildContent("Item"));

        await cut.InvokeAsync(() => cut.Instance.UpdateLayout(200, 50));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(8, cut.FindAll(".bfm-child").Count);
        });
    }

    [Fact]
    public async Task TriggersCallbacksOnAnimationEvents()
    {
        var jsRuntime = new StubJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cycles = 0;
        var finished = 0;

        var cut = RenderComponent<Marquee>(parameters => parameters
            .Add(p => p.Loop, 1)
            .Add(p => p.OnCycleComplete, EventCallback.Factory.Create(this, () => cycles++))
            .Add(p => p.OnFinish, EventCallback.Factory.Create(this, () => finished++))
            .AddChildContent("Demo"));

        await cut.Find(".bfm-marquee").TriggerEventAsync("onanimationiteration", new AnimationIterationEventArgs());
        await cut.Find(".bfm-marquee").TriggerEventAsync("onanimationend", new AnimationEventArgs());

        Assert.Equal(1, cycles);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void AppliesClassNameAndAdditionalAttributes()
    {
        var jsRuntime = new StubJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cut = RenderComponent<Marquee>(parameters => parameters
            .Add(p => p.ClassName, "custom")
            .AddUnmatched("data-testid", "marquee")
            .AddUnmatched("class", "extra")
            .AddUnmatched("style", "background:red;")
            .AddChildContent("Styled"));

        var container = cut.Find(".bfm-marquee-container");
        Assert.Contains("custom", container.ClassList);
        Assert.Contains("extra", container.ClassList);
        Assert.Equal("marquee", container.GetAttribute("data-testid"));
        Assert.Contains("background:red", container.GetAttribute("style"));
    }

    [Fact]
    public void InvokesOnMountOnlyOnce()
    {
        var jsRuntime = new StubJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var mountCount = 0;

        var cut = RenderComponent<Marquee>(parameters => parameters
            .Add(p => p.OnMount, EventCallback.Factory.Create(this, () => mountCount++))
            .AddChildContent("Mounted"));

        cut.WaitForAssertion(() => Assert.Equal(1, mountCount));

        cut.Render();
        cut.WaitForAssertion(() => Assert.Equal(1, mountCount));
    }

    private sealed class StubJsRuntime : IJSRuntime
    {
        private readonly StubModule _module = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, default, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import" && typeof(TValue) == typeof(IJSObjectReference))
            {
                return ValueTask.FromResult((TValue)(object)_module);
            }

            throw new NotSupportedException($"Unexpected identifier '{identifier}'.");
        }

        private sealed class StubModule : IJSObjectReference
        {
            private readonly StubObserver _observer = new();

            public ValueTask DisposeAsync()
                => _observer.DisposeAsync();

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
                => InvokeAsync<TValue>(identifier, default, args);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                if (identifier == "observe" && typeof(TValue) == typeof(IJSObjectReference))
                {
                    return ValueTask.FromResult((TValue)(object)_observer);
                }

                if (identifier == "measure")
                {
                    return ValueTask.FromResult(default(TValue)!);
                }

                throw new NotSupportedException($"Unexpected module call '{identifier}'.");
            }
        }

        private sealed class StubObserver : IJSObjectReference
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
                => InvokeAsync<TValue>(identifier, default, args);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                if (identifier == "update" || identifier == "dispose")
                {
                    return ValueTask.FromResult(default(TValue)!);
                }

                throw new NotSupportedException($"Unexpected observer call '{identifier}'.");
            }
        }
    }
}
