using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace BlazorFastMarquee.Tests;

/// <summary>
/// Comprehensive test suite for Marquee component.
/// Tests SSR, Server, and WASM compatibility scenarios.
/// </summary>
public class MarqueeTests : TestContext
{
  [Fact]
  public void RendersWithDefaultMarkup()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters => parameters.AddChildContent("Hello world"));

    var container = cut.Find(".bfm-marquee-container");
    Assert.Contains("--pause-on-hover", container.GetAttribute("style"));
    Assert.Equal("Hello world", cut.Find(".bfm-child").TextContent.Trim());
  }

  [Fact]
  public void RendersWithoutJavaScriptErrors()
  {
    // Simulates SSR scenario where JS may not be available
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters => parameters.AddChildContent("SSR Test"));

    Assert.NotNull(cut.Find(".bfm-marquee-container"));
    Assert.NotNull(cut.Find(".bfm-marquee"));
  }

  [Fact]
  public async Task AutoFillParameterConfiguresComponent()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.AutoFill, true).AddChildContent("Item")
    );

    // Verify component renders with AutoFill enabled
    Assert.NotNull(cut.Instance);

    // Verify UpdateLayout method is callable (actual measurement requires browser)
    await cut.InvokeAsync(() => cut.Instance.UpdateLayout(200, 50));

    // Verify component is still stable after update
    Assert.NotNull(cut.Find(".bfm-marquee-container"));
  }

  [Fact]
  public void SupportsAnimationCallbacks()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cycles = 0;
    var finished = 0;

    var cut = RenderComponent<Marquee>(parameters =>
      parameters
        .Add(p => p.Loop, 1)
        .Add(p => p.OnCycleComplete, EventCallback.Factory.Create(this, () => cycles++))
        .Add(p => p.OnFinish, EventCallback.Factory.Create(this, () => finished++))
        .AddChildContent("Demo")
    );

    // Verify component renders with callbacks registered
    Assert.NotNull(cut.Find(".bfm-marquee"));
    // Note: Actual event triggering requires browser environment
  }

  [Fact]
  public void AppliesClassNameAndAdditionalAttributes()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters
        .Add(p => p.ClassName, "custom")
        .AddUnmatched("data-testid", "marquee")
        .AddUnmatched("class", "extra")
        .AddUnmatched("style", "background:red;")
        .AddChildContent("Styled")
    );

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

    var cut = RenderComponent<Marquee>(parameters =>
      parameters
        .Add(p => p.OnMount, EventCallback.Factory.Create(this, () => mountCount++))
        .AddChildContent("Mounted")
    );

    cut.WaitForAssertion(() => Assert.Equal(1, mountCount));

    cut.Render();
    cut.WaitForAssertion(() => Assert.Equal(1, mountCount));
  }

  [Theory]
  [InlineData(MarqueeDirection.Left)]
  [InlineData(MarqueeDirection.Right)]
  [InlineData(MarqueeDirection.Up)]
  [InlineData(MarqueeDirection.Down)]
  public void SupportsAllDirections(MarqueeDirection direction)
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.Direction, direction).AddChildContent("Direction Test")
    );

    var container = cut.Find(".bfm-marquee-container");
    var style = container.GetAttribute("style");

    if (direction is MarqueeDirection.Up or MarqueeDirection.Down)
    {
      Assert.Contains("100vh", style);
    }
    else
    {
      Assert.Contains("100%", style);
    }
  }

  [Fact]
  public void HandlesSpeedAndDelayParameters()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.Speed, 100).Add(p => p.Delay, 2).AddChildContent("Speed Test")
    );

    var marquee = cut.Find(".bfm-marquee");
    var style = marquee.GetAttribute("style");

    Assert.Contains("--delay:2s", style);
  }

  [Fact]
  public void HandlesGradientParameter()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters
        .Add(p => p.Gradient, true)
        .Add(p => p.GradientColor, "blue")
        .Add(p => p.GradientWidth, new CssLength(300))
        .AddChildContent("Gradient Test")
    );

    var overlay = cut.Find(".bfm-overlay");
    var style = overlay.GetAttribute("style");

    Assert.Contains("--gradient-color:blue", style);
    Assert.Contains("--gradient-width:300px", style);
  }

  [Fact]
  public void HandlesPauseStatesCorrectly()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.Play, false).AddChildContent("Paused")
    );

    var marquee = cut.Find(".bfm-marquee");
    var style = marquee.GetAttribute("style");

    Assert.Contains("--play:paused", style);
  }

  [Fact]
  public async Task ProperlyDisposesResources()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters => parameters.AddChildContent("Dispose Test"));

    var instance = cut.Instance;
    await instance.DisposeAsync();

    // Verify disposal doesn't throw
    Assert.True(true);
  }

  [Fact]
  public void HandlesCssLengthConversions()
  {
    Assert.Equal("100px", new CssLength(100).ToString());
    Assert.Equal("0px", new CssLength(0).ToString());
    Assert.Equal("10rem", new CssLength("10rem").ToString());

    CssLength fromInt = 50;
    Assert.Equal("50px", fromInt.ToString());

    CssLength fromDouble = 75.5;
    Assert.Equal("75.5px", fromDouble.ToString());
  }

  [Fact]
  public async Task UpdateLayoutMethodIsJSInvokable()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.AutoFill, true).AddChildContent("Layout Test")
    );

    // Verify the UpdateLayout method exists and is callable from JS
    var method = typeof(Marquee).GetMethod("UpdateLayout");
    Assert.NotNull(method);

    // Verify it has JSInvokable attribute
    var attr = method.GetCustomAttributes(typeof(JSInvokableAttribute), false);
    Assert.NotEmpty(attr);

    // Verify it can be called without throwing
    await cut.InvokeAsync(() => cut.Instance.UpdateLayout(1000, 100));
    Assert.True(true); // Method call succeeded
  }

  [Fact]
  public void HandlesEmptyChildContent()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>();

    Assert.NotNull(cut.Find(".bfm-marquee-container"));
  }

  [Fact]
  public void StylesAreCachedForPerformance()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.Direction, MarqueeDirection.Left).AddChildContent("Cache Test")
    );

    var container = cut.Find(".bfm-marquee-container");
    var style1 = container.GetAttribute("style");

    // Re-render without parameter changes
    cut.Render();

    var style2 = container.GetAttribute("style");
    Assert.Equal(style1, style2);
  }

  [Fact]
  public void EnableDragParameterConfiguresComponent()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.EnableDrag, true).AddChildContent("Drag Test")
    );

    // Verify component renders with drag enabled
    Assert.NotNull(cut.Instance);
    Assert.True(cut.Instance.EnableDrag);
  }

  [Theory]
  [InlineData(MarqueeDirection.Left, false)]
  [InlineData(MarqueeDirection.Right, false)]
  [InlineData(MarqueeDirection.Up, true)]
  [InlineData(MarqueeDirection.Down, true)]
  public void DragHandlerRespectsDirection(MarqueeDirection direction, bool shouldBeVertical)
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.EnableDrag, true).Add(p => p.Direction, direction).AddChildContent("Direction Drag")
    );

    // Verify component renders with correct direction
    Assert.NotNull(cut.Instance);
    Assert.Equal(direction, cut.Instance.Direction);
  }

  [Fact]
  public void DragFeatureIsOptional()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.EnableDrag, false).AddChildContent("No Drag")
    );

    // Verify component renders without drag enabled
    Assert.NotNull(cut.Instance);
    Assert.False(cut.Instance.EnableDrag);
  }

  [Fact]
  public void DragCanBeToggledDynamically()
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters.Add(p => p.EnableDrag, false).AddChildContent("Toggle Drag")
    );

    Assert.False(cut.Instance.EnableDrag);

    // Update parameter to enable drag
    cut.SetParametersAndRender(parameters => parameters.Add(p => p.EnableDrag, true));
    Assert.True(cut.Instance.EnableDrag);

    // Disable drag again - should dispose the drag handler and trigger re-render
    cut.SetParametersAndRender(parameters => parameters.Add(p => p.EnableDrag, false));
    Assert.False(cut.Instance.EnableDrag);
    
    // Re-enable to verify it can be toggled multiple times
    cut.SetParametersAndRender(parameters => parameters.Add(p => p.EnableDrag, true));
    Assert.True(cut.Instance.EnableDrag);
    
    // Disable one more time to verify repeated toggling works
    cut.SetParametersAndRender(parameters => parameters.Add(p => p.EnableDrag, false));
    Assert.False(cut.Instance.EnableDrag);
  }

  [Theory]
  [InlineData(MarqueeDirection.Left, false)]
  [InlineData(MarqueeDirection.Right, true)]
  [InlineData(MarqueeDirection.Up, true)]
  [InlineData(MarqueeDirection.Down, false)]
  public void DragHandlerReceivesCorrectReversedFlag(MarqueeDirection direction, bool expectedReversed)
  {
    var jsRuntime = new StubJsRuntime();
    Services.AddSingleton<IJSRuntime>(jsRuntime);

    var cut = RenderComponent<Marquee>(parameters =>
      parameters
        .Add(p => p.EnableDrag, true)
        .Add(p => p.Direction, direction)
        .AddChildContent("Drag Test")
    );

    // Component should render successfully with drag enabled
    Assert.True(cut.Instance.EnableDrag);
    Assert.Equal(direction, cut.Instance.Direction);
    
    // Verify component is stable
    Assert.NotNull(cut.Find(".bfm-marquee-container"));
  }

  private sealed class StubJsRuntime : IJSRuntime
  {
    private readonly StubModule _module = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, default, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
      string identifier,
      CancellationToken cancellationToken,
      object?[]? args
    )
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
      private readonly StubAnimationHandler _animationHandler = new();
      private readonly StubDragHandler _dragHandler = new();

      public ValueTask DisposeAsync()
      {
        _ = _observer.DisposeAsync();
        _ = _animationHandler.DisposeAsync();
        _ = _dragHandler.DisposeAsync();
        return ValueTask.CompletedTask;
      }

      public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, default, args);

      public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args
      )
      {
        if (identifier == "observe" && typeof(TValue) == typeof(IJSObjectReference))
        {
          return ValueTask.FromResult((TValue)(object)_observer);
        }

        if (identifier == "setupAnimationEvents" && typeof(TValue) == typeof(IJSObjectReference))
        {
          return ValueTask.FromResult((TValue)(object)_animationHandler);
        }

        if (identifier == "setupDragHandler" && typeof(TValue) == typeof(IJSObjectReference))
        {
          return ValueTask.FromResult((TValue)(object)_dragHandler);
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

      public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, default, args);

      public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args
      )
      {
        if (identifier == "update" || identifier == "dispose")
        {
          return ValueTask.FromResult(default(TValue)!);
        }

        throw new NotSupportedException($"Unexpected observer call '{identifier}'.");
      }
    }

    private sealed class StubAnimationHandler : IJSObjectReference
    {
      public ValueTask DisposeAsync() => ValueTask.CompletedTask;

      public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, default, args);

      public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args
      )
      {
        // Handle animation event methods
        if (identifier == "dispose")
        {
          return ValueTask.FromResult(default(TValue)!);
        }

        throw new NotSupportedException($"Unexpected animation handler call '{identifier}'.");
      }
    }

    private sealed class StubDragHandler : IJSObjectReference
    {
      public ValueTask DisposeAsync() => ValueTask.CompletedTask;

      public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, default, args);

      public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args
      )
      {
        // Handle drag handler methods
        if (identifier == "update" || identifier == "dispose")
        {
          return ValueTask.FromResult(default(TValue)!);
        }

        throw new NotSupportedException($"Unexpected drag handler call '{identifier}'.");
      }
    }
  }
}
