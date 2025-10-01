using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorFastMarquee;

public sealed partial class Marquee : ComponentBase, IAsyncDisposable
{
    private static readonly string ModulePath = $"./_content/{typeof(Marquee).Assembly.GetName().Name}/js/marquee.js";

    private readonly DotNetObjectReference<Marquee> _dotNetRef;
    private ElementReference _containerRef;
    private ElementReference _marqueeRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _observer;
    private bool _onMountInvoked;
    private double _containerSpan;
    private double _marqueeSpan;
    private int _multiplier = 1;
    private string? _additionalClass;
    private string? _additionalStyle;
    private IReadOnlyDictionary<string, object>? _attributesWithoutClassOrStyle;

    public Marquee()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter] public string? ClassName { get; set; } = string.Empty;

    [Parameter] public string? Style { get; set; } = string.Empty;

    [Parameter] public bool AutoFill { get; set; }

    [Parameter] public bool Play { get; set; } = true;

    [Parameter] public bool PauseOnHover { get; set; }

    [Parameter] public bool PauseOnClick { get; set; }

    [Parameter] public MarqueeDirection Direction { get; set; } = MarqueeDirection.Left;

    [Parameter] public double Speed { get; set; } = 50d;

    [Parameter] public double Delay { get; set; }

    [Parameter] public int Loop { get; set; }

    [Parameter] public bool Gradient { get; set; }

    [Parameter] public string GradientColor { get; set; } = "white";

    [Parameter] public CssLength GradientWidth { get; set; } = new CssLength(200);

    [Parameter] public EventCallback OnFinish { get; set; }

    [Parameter] public EventCallback OnCycleComplete { get; set; }

    [Parameter] public EventCallback OnMount { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected IReadOnlyDictionary<string, object>? AdditionalAttributesWithoutClassOrStyle
        => _attributesWithoutClassOrStyle;

    private string CssClass
        => string.Join(' ', new[] { "bfm-marquee-container", ClassName, _additionalClass }
            .Where(static value => !string.IsNullOrWhiteSpace(value)));

    private string ContainerStyle
    {
        get
        {
            var builder = new StringBuilder();
            AppendCssVariable(ref builder, "pause-on-hover", (!Play || PauseOnHover) ? "paused" : "running");
            AppendCssVariable(ref builder, "pause-on-click", (!Play || PauseOnClick || (PauseOnHover && !PauseOnClick)) ? "paused" : "running");
            AppendCssVariable(ref builder, "width", Direction is MarqueeDirection.Up or MarqueeDirection.Down ? "100vh" : "100%");
            AppendCssVariable(ref builder, "container-transform", Direction switch
            {
                MarqueeDirection.Up => "rotate(-90deg)",
                MarqueeDirection.Down => "rotate(90deg)",
                _ => "none"
            });
            AppendRawStyle(ref builder, Style);
            AppendRawStyle(ref builder, _additionalStyle);
            return builder.ToString();
        }
    }

    private string GradientStyle
    {
        get
        {
            var builder = new StringBuilder();
            AppendCssVariable(ref builder, "gradient-color", GradientColor);
            AppendCssVariable(ref builder, "gradient-width", GradientWidth.ToString());
            return builder.ToString();
        }
    }

    private string MarqueeStyle
    {
        get
        {
            var builder = new StringBuilder();
            AppendCssVariable(ref builder, "play-state", Play ? "running" : "paused");
            AppendCssVariable(ref builder, "direction", Direction is MarqueeDirection.Left or MarqueeDirection.Up ? "normal" : "reverse");
            AppendCssVariable(ref builder, "duration", $"{GetDuration():0.###}s");
            AppendCssVariable(ref builder, "delay", $"{Math.Max(0, Delay):0.###}s");
            AppendCssVariable(ref builder, "iteration-count", Loop > 0 ? Loop.ToString(CultureInfo.InvariantCulture) : "infinite");
            AppendCssVariable(ref builder, "min-width", AutoFill ? "auto" : "100%");
            return builder.ToString();
        }
    }

    private string ChildStyle
    {
        get
        {
            var builder = new StringBuilder();
            AppendCssVariable(ref builder, "child-transform", Direction switch
            {
                MarqueeDirection.Up => "rotate(90deg)",
                MarqueeDirection.Down => "rotate(-90deg)",
                _ => "none"
            });
            return builder.ToString();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await EnsureModuleAsync();

        if (_module is null)
        {
            return;
        }

        await EnsureObserverAsync();
        await MeasureAsync();

        if (firstRender && OnMount.HasDelegate && !_onMountInvoked)
        {
            _onMountInvoked = true;
            await OnMount.InvokeAsync();
        }
    }

    [JSInvokable]
    public Task UpdateLayout(double containerSpan, double marqueeSpan)
    {
        if (ApplyLayout(containerSpan, marqueeSpan))
        {
            return InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private async Task EnsureModuleAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    private async Task EnsureObserverAsync()
    {
        if (_module is null)
        {
            return;
        }

        var vertical = IsVertical(Direction);

        if (_observer is null)
        {
            _observer = await _module.InvokeAsync<IJSObjectReference>("observe", _containerRef, _marqueeRef, vertical, _dotNetRef);
        }
        else
        {
            await _observer.InvokeVoidAsync("update", vertical);
        }
    }

    private async Task DisposeObserverAsync()
    {
        if (_observer is null)
        {
            return;
        }

        try
        {
            await _observer.InvokeVoidAsync("dispose");
        }
        finally
        {
            await _observer.DisposeAsync();
            _observer = null;
        }
    }

    private async Task MeasureAsync()
    {
        if (_module is null)
        {
            return;
        }

        var vertical = IsVertical(Direction);
        var measurement = await _module.InvokeAsync<MarqueeMeasurement>("measure", _containerRef, _marqueeRef, vertical);
        if (ApplyLayout(measurement.ContainerSpan, measurement.MarqueeSpan))
        {
            await InvokeAsync(StateHasChanged);
        }
    }

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

        var newMultiplier = AutoFill && marqueeSpan > 0d
            ? Math.Max(1, (int)Math.Ceiling(containerSpan / marqueeSpan))
            : 1;

        if (newMultiplier != _multiplier)
        {
            _multiplier = newMultiplier;
            hasChanged = true;
        }

        return hasChanged;
    }

    private void HandleIteration(AnimationIterationEventArgs args)
    {
        if (OnCycleComplete.HasDelegate)
        {
            _ = OnCycleComplete.InvokeAsync();
        }
    }

    private void HandleAnimationEnd(AnimationEventArgs args)
    {
        if (Loop > 0 && OnFinish.HasDelegate)
        {
            _ = OnFinish.InvokeAsync();
        }
    }

    private double GetDuration()
    {
        var effectiveSpeed = Math.Max(1d, Speed);
        if (_marqueeSpan <= 0d)
        {
            return 0d;
        }

        if (AutoFill)
        {
            return (_marqueeSpan * Math.Max(1, _multiplier)) / effectiveSpeed;
        }

        var span = _marqueeSpan < _containerSpan ? _containerSpan : _marqueeSpan;
        return span / effectiveSpeed;
    }

    protected override void OnParametersSet()
    {
        if (AdditionalAttributes is null)
        {
            _additionalClass = null;
            _additionalStyle = null;
            _attributesWithoutClassOrStyle = null;
            return;
        }

        AdditionalAttributes.TryGetValue("class", out var classAttribute);
        AdditionalAttributes.TryGetValue("style", out var styleAttribute);

        _additionalClass = classAttribute as string;
        _additionalStyle = styleAttribute as string;

        if (_additionalClass is null && _additionalStyle is null)
        {
            _attributesWithoutClassOrStyle = AdditionalAttributes;
            return;
        }

        var filtered = new Dictionary<string, object>(AdditionalAttributes);
        if (_additionalClass is not null)
        {
            filtered.Remove("class");
        }

        if (_additionalStyle is not null)
        {
            filtered.Remove("style");
        }

        _attributesWithoutClassOrStyle = filtered;
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef.Dispose();
        await DisposeObserverAsync();

        if (_module is not null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }

    private static bool IsVertical(MarqueeDirection direction)
        => direction is MarqueeDirection.Up or MarqueeDirection.Down;

    private static void AppendCssVariable(ref StringBuilder builder, string name, string value)
    {
        builder.Append("--");
        builder.Append(name);
        builder.Append(':');
        builder.Append(value);
        builder.Append(';');
    }

    private static void AppendRawStyle(ref StringBuilder builder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (builder.Length > 0 && builder[^1] != ';')
        {
            builder.Append(';');
        }

        builder.Append(trimmed);
        if (!trimmed.EndsWith(';'))
        {
            builder.Append(';');
        }
    }

    private static bool AreClose(double left, double right)
        => Math.Abs(left - right) < 0.1d;

    private readonly record struct MarqueeMeasurement(double ContainerSpan, double MarqueeSpan);
}
