namespace BlazorFastMarquee;

public partial class Marquee
{
  #region Style Building Methods (Allocation-Aware)

  private string BuildCssClass()
  {
    var hasClassName = !string.IsNullOrWhiteSpace(ClassName);
    var hasAdditional = !string.IsNullOrWhiteSpace(_additionalClass);

    // Fast paths for common cases (minimize allocations)
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

    // Use CSS variable names that match react-fast-marquee
    var pauseOnHover = (!Play || PauseOnHover) ? AnimationPaused : AnimationRunning;
    var pauseOnClick =
      (!Play || PauseOnClick || (PauseOnHover && !PauseOnClick)) ? AnimationPaused : AnimationRunning;
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

    AppendCssVariable(builder, "gradient-color", GradientColor);
    AppendCssVariable(builder, "gradient-width", GradientWidth.ToString());

    return builder.ToString();
  }

  private string BuildMarqueeStyle()
  {
    var builder = GetStringBuilder(256);

    var playState = Play ? AnimationRunning : AnimationPaused;
    var direction =
      (Direction is MarqueeDirection.Left or MarqueeDirection.Up) ? DirectionNormal : DirectionReverse;
    var minWidth = AutoFill ? MinWidthAuto : WidthFull;
    var iterationCount =
      Loop > 0 ? Loop.ToString(CultureInfo.InvariantCulture) : IterationInfinite;

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
    AppendCssVariable(builder, "transform", GetChildTransform(Direction));
    return builder.ToString();
  }

  // Reuse a StringBuilder per-component instance to reduce allocations
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

  private static void AppendCssVariable(StringBuilder builder, string name, string value)
  {
    builder.Append("--");
    builder.Append(name);
    builder.Append(':');
    builder.Append(value);
    builder.Append(';');
  }

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

  #endregion Style Building Methods (Allocation-Aware)
}

