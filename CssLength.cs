using System.Globalization;

namespace BlazorFastMarquee;

public readonly record struct CssLength
{
    private readonly string? _value;

    public CssLength(double value)
    {
        _value = Format(value);
    }

    public CssLength(string value)
    {
        _value = string.IsNullOrWhiteSpace(value) ? "0px" : value;
    }

    public override string ToString() => _value ?? "0px";

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture) + "px";

    public static implicit operator CssLength(double value) => new(value);

    public static implicit operator CssLength(int value) => new(value);

    public static implicit operator CssLength(string value) => new(value);
}
