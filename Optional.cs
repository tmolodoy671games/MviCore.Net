using System;

namespace MviCore.Net;

/// <summary>An optional result, including support for value-type actions and news.</summary>
public readonly struct Optional<T>
{
    private readonly T value;

    private Optional(T value)
    {
        this.value = value;
        HasValue = true;
    }

    /// <summary>Whether a value is present.</summary>
    public bool HasValue { get; }

    /// <summary>The contained value; throws if this result is empty.</summary>
    public T Value => HasValue ? value : throw new InvalidOperationException("No value is present.");

    /// <summary>An empty result.</summary>
    public static Optional<T> None => default;

    /// <summary>Creates a present result. Null values are not supported.</summary>
    public static Optional<T> Some(T value) => value is null
        ? throw new ArgumentNullException(nameof(value))
        : new Optional<T>(value);

    /// <summary>Converts a value to a result; null means no result.</summary>
    public static implicit operator Optional<T>(T value) => value is null ? None : Some(value);
}

/// <summary>An uninhabited type for features that never publish news.</summary>
public sealed class Nothing
{
    private Nothing() { }
}
