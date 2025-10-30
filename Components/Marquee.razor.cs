using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

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
  private CancellationTokenSource? _cts;

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
  private bool _enableDragChanged;
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
    _cts = new CancellationTokenSource();
  }

  #endregion Constructor

  #region Parameters

  [Inject]
  public ILogger<Marquee> Logger { get; set; } = default!;

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

  protected override bool ShouldRender()
  {
    // Only render if parameters actually changed
    // This prevents cascade renders from parent components
    var shouldRender =
      _classInvalidated
      || _containerStyleInvalidated
      || _gradientStyleInvalidated
      || _marqueeStyleInvalidated
      || _childStyleInvalidated
      || _enableDragChanged;

    return shouldRender;
  }

  protected override void OnParametersSet()
  {
    // Granular invalidation - only invalidate what changed
    InvalidateCachesForParameterChanges();
    ProcessAdditionalAttributes();
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    // Guard against disposal during async operations
    if (_isDisposed)
      return;

    Logger.LogInformation(
      $"[Marquee] OnAfterRenderAsync called - firstRender: {firstRender}, EnableDrag: {EnableDrag}, _dragHandler: {_dragHandler != null}"
    );

    try
    {
      if (!await EnsureModuleAsync())
        return;

      await EnsureObserverAsync();
      await EnsureAnimationHandlerAsync();

      // Always ensure drag handler is in correct state
      await EnsureDragHandlerAsync();

      // Reset the enableDragChanged flag after handling
      _enableDragChanged = false;

      await MeasureAsync();

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

    Logger.LogInformation(
      $"[Marquee] OnAfterRenderAsync complete - EnableDrag: {EnableDrag}, _dragHandler: {_dragHandler != null}"
    );
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

    var isVertical = IsVertical(Direction);
    var isReversed = IsReversedDirection(Direction);

    try
    {
      if (_dragHandler is null && EnableDrag)
      {
        Logger.LogInformation($"[Marquee] Creating drag handler - EnableDrag: {EnableDrag}");
        _dragHandler = await _module.InvokeAsync<IJSObjectReference>(
          "setupDragHandler",
          _containerRef,
          _marqueeAnimationRef,
          isVertical,
          isReversed
        );
        Logger.LogInformation($"[Marquee] Drag handler created: {_dragHandler != null}");
      }
      else if (_dragHandler is not null && EnableDrag)
      {
        Logger.LogInformation($"[Marquee] Updating drag handler - EnableDrag: {EnableDrag}");
        await _dragHandler.InvokeVoidAsync("update", isVertical, isReversed);
      }
      else if (_dragHandler is not null && !EnableDrag)
      {
        Logger.LogInformation(
          $"[Marquee] Disposing drag handler - EnableDrag: {EnableDrag}, _dragHandler: {_dragHandler != null}"
        );
        await DisposeDragHandlerAsync();
        Logger.LogInformation(
          $"[Marquee] Drag handler disposed - _dragHandler is now: {_dragHandler == null}"
        );
      }
      else
      {
        Logger.LogInformation(
          $"[Marquee] No action - EnableDrag: {EnableDrag}, _dragHandler: {_dragHandler != null}"
        );
      }
    }
    catch (JSDisconnectedException)
    {
      // Circuit disconnected - cleanup
      Logger.LogInformation("[Marquee] JS disconnected");
      _dragHandler = null;
    }
    catch (TaskCanceledException)
    {
      // Expected during disposal
      Logger.LogInformation("[Marquee] Task cancelled");
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

  #region Style Building Methods (Zero-Allocation Optimized)

  private string BuildCssClass()
  {
    var hasClassName = !string.IsNullOrWhiteSpace(ClassName);
    var hasAdditional = !string.IsNullOrWhiteSpace(_additionalClass);

    // Fast paths for common cases (zero allocation)
    if (!hasClassName && !hasAdditional)
      return BaseClassName;

    if (!hasAdditional)
      return string.Concat(BaseClassName, " ", ClassName);

    if (!hasClassName)
      return string.Concat(BaseClassName, " ", _additionalClass);

    return string.Concat(BaseClassName, " ", ClassName, " ", _additionalClass);
  }

  private string BuildContainerStyle()
  {
    var builder = GetStringBuilder(256);

    // Use CSS variable names that match React Fast Marquee
    var pauseOnHover = (!Play || PauseOnHover) ? AnimationPaused : AnimationRunning;
    var pauseOnClick =
      (!Play || PauseOnClick || (PauseOnHover && !PauseOnClick))
        ? AnimationPaused
        : AnimationRunning;
    var width = IsVertical(Direction) ? WidthViewport : WidthFull;
    var transform = GetContainerTransform(Direction);

    AppendCssVariable(builder, "pause-on-hover", pauseOnHover);
    AppendCssVariable(builder, "pause-on-click", pauseOnClick);
    AppendCssVariable(builder, "width", width);
    AppendCssVariable(builder, "transform", transform);
    AppendRawStyle(builder, Style);
    AppendRawStyle(builder, _additionalStyle);

    return builder.ToString();
  }

  private string BuildGradientStyle()
  {
    var builder = GetStringBuilder(128);

    // Convert CssLength to proper CSS value
    var gradientWidthValue = GradientWidth.ToString();

    AppendCssVariable(builder, "gradient-color", GradientColor);
    AppendCssVariable(builder, "gradient-width", gradientWidthValue);

    return builder.ToString();
  }

  private string BuildMarqueeStyle()
  {
    var builder = GetStringBuilder(256);

    // Use CSS variable names that match React Fast Marquee
    var playState = Play ? AnimationRunning : AnimationPaused;
    var direction =
      (Direction is MarqueeDirection.Left or MarqueeDirection.Up)
        ? DirectionNormal
        : DirectionReverse;
    var minWidth = AutoFill ? MinWidthAuto : WidthFull;
    var iterationCount = Loop > 0 ? Loop.ToString(CultureInfo.InvariantCulture) : IterationInfinite;

    AppendCssVariable(builder, "play", playState);
    AppendCssVariable(builder, "direction", direction);
    AppendCssVariableNumber(builder, "duration", GetDuration());
    AppendCssVariableNumber(builder, "delay", Math.Max(0, Delay));
    AppendCssVariable(builder, "iteration-count", iterationCount);
    AppendCssVariable(builder, "min-width", minWidth);

    return builder.ToString();
  }

  private string BuildChildStyle()
  {
    var builder = GetStringBuilder(64);
    var transform = GetChildTransform(Direction);

    AppendCssVariable(builder, "transform", transform);

    return builder.ToString();
  }

  #endregion Style Building Methods (Zero-Allocation Optimized)

  #region Layout Calculation Methods

  private bool ApplyLayout(double containerSpan, double marqueeSpan)
  {
    var hasChanged = false;

    if (!AreClose(containerSpan, _containerSpan))
    {
      _containerSpan = containerSpan;
      hasChanged = true;
    }

    if (!AreClose(marqueeSpan, _marqueeSpan))
    {
      _marqueeSpan = marqueeSpan;
      hasChanged = true;
    }

    var newMultiplier = CalculateMultiplier(containerSpan, marqueeSpan);
    if (newMultiplier != _multiplier)
    {
      _multiplier = newMultiplier;
      hasChanged = true;
    }

    return hasChanged;
  }

  private int CalculateMultiplier(double containerSpan, double marqueeSpan)
  {
    if (!AutoFill || marqueeSpan <= 0d)
      return 1;

    return Math.Max(1, (int)Math.Ceiling(containerSpan / marqueeSpan));
  }

  private double GetDuration()
  {
    var effectiveSpeed = Math.Max(1d, Speed);

    if (_marqueeSpan <= 0d)
      return 0d;

    // Match React Fast Marquee duration calculation
    if (AutoFill)
    {
      return (_marqueeSpan * _multiplier) / effectiveSpeed;
    }
    else
    {
      // If marquee is smaller than container, use container width for duration
      // Otherwise use marquee width
      return _marqueeSpan < _containerSpan
        ? _containerSpan / effectiveSpeed
        : _marqueeSpan / effectiveSpeed;
    }
  }

  #endregion Layout Calculation Methods

  #region Helper Methods

  private void InvalidateCachesForParameterChanges()
  {
    // Granular cache invalidation based on what actually changed
    if (_prevClassName != ClassName || _prevClassName != _additionalClass)
    {
      _classInvalidated = true;
      _prevClassName = ClassName;
    }

    if (
      _prevStyle != Style
      || _prevPlay != Play
      || _prevPauseOnHover != PauseOnHover
      || _prevPauseOnClick != PauseOnClick
      || _prevDirection != Direction
    )
    {
      _containerStyleInvalidated = true;
      _prevStyle = Style;
      _prevPlay = Play;
      _prevPauseOnHover = PauseOnHover;
      _prevPauseOnClick = PauseOnClick;
      _prevDirection = Direction;
    }

    // Track EnableDrag separately to ensure proper drag handler lifecycle
    if (_prevEnableDrag != EnableDrag)
    {
      Logger.LogInformation($"[Marquee] EnableDrag changed from {_prevEnableDrag} to {EnableDrag}");
      _enableDragChanged = true; // Flag that drag state changed
      _containerStyleInvalidated = true; // Force a re-render
      _prevEnableDrag = EnableDrag;
    }

    if (
      _prevGradient != Gradient
      || _prevGradientColor != GradientColor
      || !_prevGradientWidth.Equals(GradientWidth)
    )
    {
      _gradientStyleInvalidated = true;
      _prevGradient = Gradient;
      _prevGradientColor = GradientColor;
      _prevGradientWidth = GradientWidth;
    }

    if (
      _prevPlay != Play
      || _prevDirection != Direction
      || !AreClose(_prevSpeed, Speed)
      || !AreClose(_prevDelay, Delay)
      || _prevLoop != Loop
      || _prevAutoFill != AutoFill
    )
    {
      _marqueeStyleInvalidated = true;
      _prevSpeed = Speed;
      _prevDelay = Delay;
      _prevLoop = Loop;
      _prevAutoFill = AutoFill;
    }

    if (_prevDirection != Direction)
    {
      _childStyleInvalidated = true;
    }
  }

  private void ProcessAdditionalAttributes()
  {
    if (AdditionalAttributes is null)
    {
      _additionalClass = null;
      _additionalStyle = null;
      _attributesWithoutClassOrStyle = null;
      return;
    }

    var hasClass = AdditionalAttributes.TryGetValue("class", out var classAttribute);
    var hasStyle = AdditionalAttributes.TryGetValue("style", out var styleAttribute);

    _additionalClass = hasClass ? classAttribute as string : null;
    _additionalStyle = hasStyle ? styleAttribute as string : null;

    if (!hasClass && !hasStyle)
    {
      _attributesWithoutClassOrStyle = AdditionalAttributes;
      return;
    }

    // Lazy allocation: only create dictionary if we need to filter
    // Pre-size for efficiency
    var filtered = new Dictionary<string, object>(
      AdditionalAttributes.Count - (hasClass ? 1 : 0) - (hasStyle ? 1 : 0)
    );
    foreach (var (key, value) in AdditionalAttributes)
    {
      if (key != "class" && key != "style")
        filtered[key] = value;
    }

    _attributesWithoutClassOrStyle = filtered;
  }

  // Reuse StringBuilder to reduce allocations
  private StringBuilder GetStringBuilder(int capacity)
  {
    if (_stringBuilder is null)
    {
      _stringBuilder = new StringBuilder(capacity);
    }
    else
    {
      _stringBuilder.Clear();
      if (_stringBuilder.Capacity < capacity)
        _stringBuilder.Capacity = capacity;
    }

    return _stringBuilder;
  }

  private static bool IsVertical(MarqueeDirection direction) =>
    direction is MarqueeDirection.Up or MarqueeDirection.Down;

  private static bool IsReversedDirection(MarqueeDirection direction) =>
    direction is MarqueeDirection.Right or MarqueeDirection.Up;

  private static bool AreClose(double left, double right) => Math.Abs(left - right) < 0.1d;

  // Zero-allocation transform getters using pre-allocated strings
  private static string GetContainerTransform(MarqueeDirection direction) =>
    direction switch
    {
      MarqueeDirection.Up => RotateNeg90,
      MarqueeDirection.Down => Rotate90,
      _ => TransformNone
    };

  private static string GetChildTransform(MarqueeDirection direction) =>
    direction switch
    {
      MarqueeDirection.Up => Rotate90,
      MarqueeDirection.Down => RotateNeg90,
      _ => TransformNone
    };

  // Optimized CSS variable append (no string formatting)
  private static void AppendCssVariable(StringBuilder builder, string name, string value)
  {
    builder.Append("--");
    builder.Append(name);
    builder.Append(':');
    builder.Append(value);
    builder.Append(';');
  }

  // Specialized version for numeric values (reduces allocations)
  private static void AppendCssVariableNumber(StringBuilder builder, string name, double value)
  {
    builder.Append("--");
    builder.Append(name);
    builder.Append(':');
    builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
    builder.Append("s;");
  }

  private static void AppendRawStyle(StringBuilder builder, string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return;

    var trimmed = value.Trim();
    if (trimmed.Length == 0)
      return;

    if (builder.Length > 0 && builder[^1] != ';')
      builder.Append(';');

    builder.Append(trimmed);
    if (!trimmed.EndsWith(';'))
      builder.Append(';');
  }

  #endregion Helper Methods

  #region Disposal

  /// <summary>
  /// Disposes resources and cleans up JS interop references.
  /// Handles both WASM and Server scenarios gracefully.
  /// </summary>
  public async ValueTask DisposeAsync()
  {
    if (_isDisposed)
      return;

    _isDisposed = true;

    // Cancel any pending async operations
    _cts?.Cancel();

    // Dispose .NET object reference
    _dotNetRef?.Dispose();

    // Cleanup JS interop resources
    await DisposeDragHandlerAsync();
    await DisposeAnimationHandlerAsync();
    await DisposeObserverAsync();
    await DisposeModuleAsync();

    // Dispose cancellation token
    _cts?.Dispose();
    _cts = null;
  }

  private async ValueTask DisposeDragHandlerAsync()
  {
    if (_dragHandler is null)
    {
      Logger.LogInformation(
        "[Marquee] DisposeDragHandlerAsync called but _dragHandler is already null"
      );
      return;
    }

    var handlerToDispose = _dragHandler;
    _dragHandler = null; // Clear immediately to prevent re-entry

    Logger.LogInformation(
      "[Marquee] DisposeDragHandlerAsync: Setting _dragHandler to null and disposing"
    );

    try
    {
      await handlerToDispose.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch (Exception ex)
    {
      Logger.LogInformation($"[Marquee] Error calling dispose: {ex.Message}");
    }

    try
    {
      await handlerToDispose.DisposeAsync();
    }
    catch (Exception ex)
    {
      Logger.LogInformation($"[Marquee] Error in DisposeAsync: {ex.Message}");
    }

    Logger.LogInformation("[Marquee] DisposeDragHandlerAsync complete, _dragHandler is now null");
  }

  private async ValueTask DisposeAnimationHandlerAsync()
  {
    if (_animationHandler is null)
      return;

    try
    {
      await _animationHandler.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      try
      {
        await _animationHandler.DisposeAsync();
      }
      catch
      {
        // Suppress disposal errors
      }
      _animationHandler = null;
    }
  }

  private async ValueTask DisposeObserverAsync()
  {
    if (_observer is null)
      return;

    try
    {
      // Try to notify JS to cleanup
      await _observer.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      try
      {
        await _observer.DisposeAsync();
      }
      catch
      {
        // Suppress disposal errors
      }
      _observer = null;
    }
  }

  private async ValueTask DisposeModuleAsync()
  {
    if (_module is null)
      return;

    try
    {
      await _module.DisposeAsync();
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      _module = null;
    }
  }

  #endregion Disposal

  #region Nested Types

  private readonly record struct MarqueeMeasurement(double ContainerSpan, double MarqueeSpan);

  #endregion Nested Types
}
