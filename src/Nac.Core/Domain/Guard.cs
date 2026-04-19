namespace Nac.Core.Domain;

public static class Guard
{
    public static T NotNull<T>(T? value, string paramName) where T : class =>
        value ?? throw new ArgumentNullException(paramName);

    public static string NotNullOrEmpty(string? value, string paramName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or empty.", paramName)
            : value;

    public static string MaxLength(string value, int maxLength, string paramName) =>
        value.Length > maxLength
            ? throw new ArgumentException($"Value must not exceed {maxLength} characters.", paramName)
            : value;

    public static T NotDefault<T>(T value, string paramName) where T : struct =>
        value.Equals(default(T))
            ? throw new ArgumentException("Value cannot be default.", paramName)
            : value;

    public static decimal NotNegative(decimal value, string paramName) =>
        value < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.")
            : value;

    public static decimal Positive(decimal value, string paramName) =>
        value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be positive.")
            : value;

    public static int InRange(int value, int min, int max, string paramName) =>
        value < min || value > max
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be between {min} and {max}.")
            : value;
}
