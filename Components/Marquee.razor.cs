namespace BlazorFastMarquee;

public partial class Marquee : ComponentBase, IAsyncDisposable
{
  #region Constants and Static Fields

  private static readonly string ModulePath =
    $"./_content/{typeof(Marquee).Assembly.GetName().Name}/Components/{nameof(Marquee)}.razor.js";

  private static readonly string BaseClassName = "bfm-marquee-container";

  // Pre-allocated rotation transform strings (zero allocation)
  private static readonly string RotateNeg90 = "rotate(-90deg)";

  private static readonly string Rotate90 = "rotate(90deg)";
  private static readonly string TransformNone = "none";
  private static readonly string DirectionNormal = "normal";
  private static readonly string DirectionReverse = "reverse";
  private static readonly string AnimationRunning = "running";
  private static readonly string AnimationPaused = "paused";
  private static readonly string WidthFull = "100%";
  private static readonly string WidthViewport = "100vh";
  private static readonly string MinWidthAuto = "auto";
  private static readonly string IterationInfinite = "infinite";

  #endregion Constants and Static Fields

  #region Instance Fields

  // JS Interop references
  private readonly DotNetObjectReference<Marquee> _dotNetRef;

  private ElementReference _containerRef;
  private ElementReference _marqueeRef;
  private ElementReference _marqueeAnimationRef;
  private IJSObjectReference? _module;
  private IJSObjectReference? _observer;
  private IJSObjectReference? _animationHandler;
  private IJSObjectReference? _dragHandler;

  // Lifecycle tracking
  private bool _isDisposed;

  private bool _onMountInvoked;
  private readonly SemaphoreSlim _dragHandlerLock = new(1, 1);

  // Layout state
  private double _containerSpan;

  private double _marqueeSpan;
  private int _multiplier = 1;

  // Attribute processing
  private string? _additionalClass;

  private string? _additionalStyle;
  private IReadOnlyDictionary<string, object>? _attributesWithoutClassOrStyle;

  // Memoization: Cached style strings (zero allocation on re-render)
  private string _cachedCssClass = string.Empty;

  private string _cachedContainerStyle = string.Empty;
  private string _cachedGradientStyle = string.Empty;
  private string _cachedMarqueeStyle = string.Empty;
  private string _cachedChildStyle = string.Empty;

  // Invalidation flags for granular cache control
  private bool _classInvalidated = true;

  private bool _containerStyleInvalidated = true;
  private bool _gradientStyleInvalidated = true;
  private bool _marqueeStyleInvalidated = true;
  private bool _childStyleInvalidated = true;

  // Parameter change tracking for ShouldRender optimization
  private string? _prevClassName;

  private string? _prevStyle;
  private bool _prevAutoFill;
  private bool _prevPlay;
  private bool _prevPauseOnHover;
  private bool _prevPauseOnClick;
  private bool _prevEnableDrag;
  private MarqueeDirection _prevDirection;
  private double _prevSpeed;
  private double _prevDelay;
  private int _prevLoop;
  private bool _prevGradient;
  private string _prevGradientColor = "white";
  private CssLength _prevGradientWidth;

  // Reusable StringBuilder pool (reduces allocations)
  private StringBuilder? _stringBuilder;

  #endregion Instance Fields

  #region Constructor

  public Marquee()
  {
    _dotNetRef = DotNetObjectReference.Create(this);
  }

  #endregion Constructor

  #region Parameters

  /// <summary>CSS class name to apply to the container.</summary>
  [Parameter]
  public string? ClassName { get; set; } = string.Empty;

  /// <summary>Inline CSS styles to apply to the container.</summary>
  [Parameter]
  public string? Style { get; set; } = string.Empty;

  /// <summary>Automatically fills the container with duplicated content to avoid gaps.</summary>
  [Parameter]
  public bool AutoFill { get; set; }

  /// <summary>Controls animation playback state.</summary>
  [Parameter]
  public bool Play { get; set; } = true;

  /// <summary>Pauses animation on hover.</summary>
  [Parameter]
  public bool PauseOnHover { get; set; }

  /// <summary>Pauses animation on click.</summary>
  [Parameter]
  public bool PauseOnClick { get; set; }

  /// <summary>Enables drag to pan the marquee (respects direction).</summary>
  [Parameter]
  public bool EnableDrag { get; set; }

  /// <summary>Direction of marquee animation.</summary>
  [Parameter]
  public MarqueeDirection Direction { get; set; } = MarqueeDirection.Left;

  /// <summary>Speed in pixels per second.</summary>
  [Parameter]
  public double Speed { get; set; } = 50d;

  /// <summary>Delay before animation starts (in seconds).</summary>
  [Parameter]
  public double Delay { get; set; }

  /// <summary>Number of animation loops. 0 for infinite.</summary>
  [Parameter]
  public int Loop { get; set; }

  /// <summary>Enables gradient overlay at edges.</summary>
  [Parameter]
  public bool Gradient { get; set; }

  /// <summary>Color of the gradient overlay.</summary>
  [Parameter]
  public string GradientColor { get; set; } = "white";

  /// <summary>Width of the gradient overlay.</summary>
  [Parameter]
  public CssLength GradientWidth { get; set; } = new CssLength(200);

  /// <summary>Callback invoked when animation completes (finite loops only).</summary>
  [Parameter]
  public EventCallback OnFinish { get; set; }

  /// <summary>Callback invoked on each animation iteration.</summary>
  [Parameter]
  public EventCallback OnCycleComplete { get; set; }

  /// <summary>Callback invoked once after first render.</summary>
  [Parameter]
  public EventCallback OnMount { get; set; }

  /// <summary>Content to animate in the marquee.</summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>Additional attributes to apply to the container element.</summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  #endregion Parameters

  #region Computed Properties (Memoized)

  private IReadOnlyDictionary<string, object>? AdditionalAttributesWithoutClassOrStyle =>
    _attributesWithoutClassOrStyle;

  private string CssClass
  {
    get
    {
      if (!_classInvalidated)
        return _cachedCssClass;

      _cachedCssClass = BuildCssClass();
      _classInvalidated = false;
      return _cachedCssClass;
    }
  }

  private string ContainerStyle
  {
    get
    {
      if (!_containerStyleInvalidated)
        return _cachedContainerStyle;

      _cachedContainerStyle = BuildContainerStyle();
      _containerStyleInvalidated = false;
      return _cachedContainerStyle;
    }
  }

  private string GradientStyle
  {
    get
    {
      if (!_gradientStyleInvalidated)
        return _cachedGradientStyle;

      _cachedGradientStyle = BuildGradientStyle();
      _gradientStyleInvalidated = false;
      return _cachedGradientStyle;
    }
  }

  private string MarqueeStyle
  {
    get
    {
      if (!_marqueeStyleInvalidated)
        return _cachedMarqueeStyle;

      _cachedMarqueeStyle = BuildMarqueeStyle();
      _marqueeStyleInvalidated = false;
      return _cachedMarqueeStyle;
    }
  }

  private string ChildStyle
  {
    get
    {
      if (!_childStyleInvalidated)
        return _cachedChildStyle;

      _cachedChildStyle = BuildChildStyle();
      _childStyleInvalidated = false;
      return _cachedChildStyle;
    }
  }

  #endregion Computed Properties (Memoized)

  #region Lifecycle Methods

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (_isDisposed)
      return;

    try
    {
      if (!await EnsureModuleAsync())
        return;

      await EnsureObserverAsync();
      await EnsureAnimationHandlerAsync();
      await EnsureDragHandlerAsync();

      if (firstRender || _measureRequested || _observer is null)
      {
        await MeasureAsync();
        _measureRequested = false;
      }

      if (firstRender && OnMount.HasDelegate && !_onMountInvoked)
      {
        _onMountInvoked = true;
        await OnMount.InvokeAsync();
      }
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected in Blazor Server - gracefully handle
    }
    catch (TaskCanceledException)
    {
      // Component disposed during async operation - expected
    }
  }

  #endregion Lifecycle Methods

  #region JS Interop Methods

  [Inject]
  private IJSRuntime JS { get; init; } = default!;

  /// <summary>
  /// Updates layout measurements from JavaScript. Called via JS interop.
  /// Thread-safe and optimized for minimal re-renders.
  /// </summary>
  [JSInvokable]
  public Task UpdateLayout(double containerSpan, double marqueeSpan)
  {
    if (_isDisposed)
      return Task.CompletedTask;

    if (ApplyLayout(containerSpan, marqueeSpan))
    {
      // Only invalidate marquee style (duration depends on dimensions)
      _marqueeStyleInvalidated = true;
      return InvokeAsync(StateHasChanged);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Called from JavaScript when animation iteration event occurs
  /// </summary>
  [JSInvokable]
  public Task HandleAnimationIteration()
  {
    if (_isDisposed || !OnCycleComplete.HasDelegate)
      return Task.CompletedTask;

    return InvokeAsync(async () =>
    {
      try
      {
        await OnCycleComplete.InvokeAsync();
      }
      catch (Exception)
      {
        // Suppress exceptions from user callbacks to prevent component crash
      }
    });
  }

  /// <summary>
  /// Called from JavaScript when animation end event occurs
  /// </summary>
  [JSInvokable]
  public Task HandleAnimationEnd()
  {
    if (_isDisposed || Loop <= 0 || !OnFinish.HasDelegate)
      return Task.CompletedTask;

    return InvokeAsync(async () =>
    {
      try
      {
        await OnFinish.InvokeAsync();
      }
      catch (Exception)
      {
        // Suppress exceptions from user callbacks to prevent component crash
      }
    });
  }

  private async ValueTask<bool> EnsureModuleAsync()
  {
    if (_module is not null || _isDisposed)
      return _module is not null;

    try
    {
      _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
      return true;
    }
    catch (JSDisconnectedException)
    {
      // Blazor Server circuit disconnected
      return false;
    }
    catch (TaskCanceledException)
    {
      // Disposal during import
      return false;
    }
  }

  private async Task EnsureObserverAsync()
  {
    if (_module is null || _isDisposed)
      return;

    var isVertical = IsVertical(Direction);

    try
    {
      if (_observer is null)
      {
        _observer = await _module.InvokeAsync<IJSObjectReference>(
          "observe",
          _containerRef,
          _marqueeRef,
          isVertical,
          _dotNetRef
        );
      }
      else
      {
        await _observer.InvokeVoidAsync("update", isVertical);
      }
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected - cleanup
      _observer = null;
    }
    catch (TaskCanceledException)
    {
      // Expected during disposal
    }
  }

  private async Task EnsureAnimationHandlerAsync()
  {
    if (_module is null || _isDisposed)
      return;

    try
    {
      _animationHandler ??= await _module.InvokeAsync<IJSObjectReference>(
        "setupAnimationEvents",
        _marqueeAnimationRef,
        _dotNetRef
      );
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected - cleanup
      _animationHandler = null;
    }
    catch (TaskCanceledException)
    {
      // Expected during disposal
    }
  }

  private async Task EnsureDragHandlerAsync()
  {
    if (_module is null || _isDisposed)
      return;

    // Use semaphore to prevent concurrent execution from multiple render cycles
    await _dragHandlerLock.WaitAsync();
    try
    {
      var isVertical = IsVertical(Direction);
      var isReversed = IsReversedDirection(Direction);

      if (_dragHandler is null && EnableDrag)
      {
        _dragHandler = await _module.InvokeAsync<IJSObjectReference>(
          "setupDragHandler",
          _containerRef,
          _marqueeAnimationRef,
          isVertical,
          isReversed
        );
      }
      else if (_dragHandler is not null && EnableDrag)
      {
        await _dragHandler.InvokeVoidAsync("update", isVertical, isReversed);
      }
      else if (_dragHandler is not null && !EnableDrag)
      {
        await DisposeDragHandlerAsync();
      }
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected - cleanup
      _dragHandler = null;
    }
    catch (TaskCanceledException)
    {
      // Expected during disposal
    }
    finally
    {
      _dragHandlerLock.Release();
    }
  }

  private async Task MeasureAsync()
  {
    if (_module is null || _isDisposed)
      return;

    try
    {
      var isVertical = IsVertical(Direction);
      var measurement = await _module.InvokeAsync<MarqueeMeasurement>(
        "measure",
        _containerRef,
        _marqueeRef,
        isVertical
      );

      if (ApplyLayout(measurement.ContainerSpan, measurement.MarqueeSpan))
      {
        _marqueeStyleInvalidated = true;
        await InvokeAsync(StateHasChanged);
      }
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected - skip measurement
    }
    catch (TaskCanceledException)
    {
      // Expected during disposal
    }
  }

  #endregion JS Interop Methods

  #region Nested Types

  private readonly record struct MarqueeMeasurement(double ContainerSpan, double MarqueeSpan);

  #endregion Nested Types
}
