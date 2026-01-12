namespace BlazorFastMarquee;

public partial class Marquee
{
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

    // Match react-fast-marquee duration calculation.
    if (AutoFill)
      return (_marqueeSpan * _multiplier) / effectiveSpeed;

    // If marquee is smaller than container, use container width for duration,
    // otherwise use marquee width.
    return _marqueeSpan < _containerSpan ? _containerSpan / effectiveSpeed : _marqueeSpan / effectiveSpeed;
  }

  private static bool IsVertical(MarqueeDirection direction) =>
    direction is MarqueeDirection.Up or MarqueeDirection.Down;

  private static bool IsReversedDirection(MarqueeDirection direction) =>
    direction is MarqueeDirection.Right or MarqueeDirection.Up;

  private static bool AreClose(double left, double right) => Math.Abs(left - right) < 0.1d;

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

  #endregion Layout Calculation Methods
}

