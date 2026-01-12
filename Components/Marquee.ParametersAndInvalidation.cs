namespace BlazorFastMarquee;

public partial class Marquee
{
  private string? _prevAdditionalClass;
  private string? _prevAdditionalStyle;
  private RenderFragment? _prevChildContent;
  private bool _measureRequested = true;

  protected override void OnParametersSet()
  {
    // AdditionalAttributes influence class/style and attribute forwarding; process first.
    ProcessAdditionalAttributes();
    InvalidateCachesForParameterChanges();
  }

  private void InvalidateCachesForParameterChanges()
  {
    var classChanged = _prevClassName != ClassName || _prevAdditionalClass != _additionalClass;
    var containerStyleChanged =
      _prevStyle != Style
      || _prevPlay != Play
      || _prevPauseOnHover != PauseOnHover
      || _prevPauseOnClick != PauseOnClick
      || _prevDirection != Direction
      || _prevEnableDrag != EnableDrag
      || _prevAdditionalStyle != _additionalStyle;

    var gradientStyleChanged =
      _prevGradient != Gradient
      || _prevGradientColor != GradientColor
      || !_prevGradientWidth.Equals(GradientWidth);

    var marqueeStyleChanged =
      _prevPlay != Play
      || _prevDirection != Direction
      || !AreClose(_prevSpeed, Speed)
      || !AreClose(_prevDelay, Delay)
      || _prevLoop != Loop
      || _prevAutoFill != AutoFill;

    var childStyleChanged = _prevDirection != Direction;
    var childContentChanged = !ReferenceEquals(_prevChildContent, ChildContent);

    if (classChanged)
      _classInvalidated = true;

    if (containerStyleChanged)
      _containerStyleInvalidated = true;

    if (gradientStyleChanged)
      _gradientStyleInvalidated = true;

    if (marqueeStyleChanged)
      _marqueeStyleInvalidated = true;

    if (childStyleChanged)
      _childStyleInvalidated = true;

    // If content changes, measurement may change; request a measure next render.
    if (childContentChanged || childStyleChanged)
      _measureRequested = true;

    // Update previous values after computing change flags.
    _prevClassName = ClassName;
    _prevAdditionalClass = _additionalClass;
    _prevStyle = Style;
    _prevAdditionalStyle = _additionalStyle;
    _prevPlay = Play;
    _prevPauseOnHover = PauseOnHover;
    _prevPauseOnClick = PauseOnClick;
    _prevEnableDrag = EnableDrag;
    _prevDirection = Direction;
    _prevSpeed = Speed;
    _prevDelay = Delay;
    _prevLoop = Loop;
    _prevAutoFill = AutoFill;
    _prevGradient = Gradient;
    _prevGradientColor = GradientColor;
    _prevGradientWidth = GradientWidth;
    _prevChildContent = ChildContent;
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

    // Only allocate when filtering is required.
    var filtered = new Dictionary<string, object>(
      AdditionalAttributes.Count - (hasClass ? 1 : 0) - (hasStyle ? 1 : 0)
    );

    foreach (var (key, value) in AdditionalAttributes)
    {
      if (key is not ("class" or "style"))
        filtered[key] = value;
    }

    _attributesWithoutClassOrStyle = filtered;
  }
}

