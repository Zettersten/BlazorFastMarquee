using System.Globalization;

namespace BlazorFastMarquee;

/// <summary>
/// Represents a CSS length value with automatic unit handling.
/// Supports implicit conversion from numeric types and strings.
/// </summary>
public readonly record struct CssLength
{
    private readonly string? _value;

    /// <summary>
    /// Creates a CSS length from a numeric value with 'px' units.
    /// </summary>
    public CssLength(double value)
    {
        _value = Format(value);
    }

    /// <summary>
    /// Creates a CSS length from a string value (e.g., "10px", "5rem").
    /// </summary>
    public CssLength(string value)
    {
        _value = string.IsNullOrWhiteSpace(value) ? "0px" : value;
    }

    /// <summary>
    /// Returns the CSS length value as a string.
    /// </summary>
    public override string ToString() => _value ?? "0px";

    private static string Format(double value)
    {
        // Avoid allocations for common values
        return value switch
        {
            0 => "0px",
            _ => string.Create(CultureInfo.InvariantCulture, $"{value:0.###}px")
        };
    }

    /// <summary>Implicit conversion from double to CssLength.</summary>
    public static implicit operator CssLength(double value) => new(value);

    /// <summary>Implicit conversion from int to CssLength.</summary>
    public static implicit operator CssLength(int value) => new(value);

    /// <summary>Implicit conversion from string to CssLength.</summary>
    public static implicit operator CssLength(string value) => new(value);
}
