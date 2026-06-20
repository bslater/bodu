// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertyValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Represents a single typed value read from an OLE property set, the managed counterpart of a COM <c>PROPVARIANT</c>.
/// </summary>
/// <remarks>
/// The decoded CLR value is exposed through <see cref="Value" /> and the strongly-typed accessor methods. Scalar values
/// are boxed to their natural CLR type (for example <see cref="int" />, <see cref="string" />, or
/// <see cref="DateTimeOffset" />); vector values (<see cref="IsVector" />) are exposed as an <see cref="object" />
/// array of element values. A Windows FILETIME is stored as its raw 100-nanosecond tick count, so it can be read either
/// as a <see cref="DateTimeOffset" /> (a point in time) or a <see cref="TimeSpan" /> (an elapsed duration, as used by
/// the total-edit-time property).
/// </remarks>
public sealed record OlePropertyValue
{
    /// <summary>
    /// Gets the scalar value type of this property.
    /// </summary>
    /// <returns>The <see cref="OlePropertyType" /> describing the decoded value.</returns>
    public OlePropertyType Type { get; init; }

    /// <summary>
    /// Gets a value indicating whether this property carries a vector (array) of values rather than a single scalar.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Value" /> is an <see cref="object" /> array; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool IsVector { get; init; }

    /// <summary>
    /// Gets the decoded value.
    /// </summary>
    /// <returns>
    /// The boxed CLR value for a scalar property, an <see cref="object" /> array for a vector property, or
    /// <see langword="null" /> for an empty or null value.
    /// </returns>
    public object? Value { get; init; }

    /// <summary>
    /// Returns the value as a string when it is a string scalar.
    /// </summary>
    /// <returns>The string value, or <see langword="null" /> when the value is not a string.</returns>
    public string? AsString() =>
        Value as string;

    /// <summary>
    /// Returns the value as a 32-bit integer when it is an integral scalar.
    /// </summary>
    /// <returns>The integer value, or <see langword="null" /> when the value is not integral.</returns>
    public int? AsInt32() =>
        Value switch
        {
            sbyte v => v,
            byte v => v,
            short v => v,
            ushort v => v,
            int v => v,
            uint v => (int)v,
            long v => (int)v,
            ulong v => (int)v,
            _ => null,
        };

    /// <summary>
    /// Returns the value as a 64-bit integer when it is an integral scalar.
    /// </summary>
    /// <returns>The integer value, or <see langword="null" /> when the value is not integral.</returns>
    public long? AsInt64() =>
        Value switch
        {
            sbyte v => v,
            byte v => v,
            short v => v,
            ushort v => v,
            int v => v,
            uint v => v,
            long v => v,
            ulong v => (long)v,
            _ => null,
        };

    /// <summary>
    /// Returns the value as a boolean when it is a boolean scalar.
    /// </summary>
    /// <returns>The boolean value, or <see langword="null" /> when the value is not a boolean.</returns>
    public bool? AsBoolean() =>
        Value as bool?;

    /// <summary>
    /// Returns the value as a point in time when it is a date or FILETIME scalar.
    /// </summary>
    /// <returns>The time value, or <see langword="null" /> when the value is not a date or FILETIME.</returns>
    public DateTimeOffset? AsDateTimeOffset()
    {
        if (Value is DateTimeOffset dto)
            return dto;

        if (Type == OlePropertyType.FileTime && Value is long ticks && ticks > 0 && ticks <= s_maxFileTime)
            return new DateTimeOffset(DateTime.FromFileTimeUtc(ticks), TimeSpan.Zero);

        return null;
    }

    /// <summary>
    /// Returns the value as an elapsed duration when it is a FILETIME scalar.
    /// </summary>
    /// <returns>The duration, or <see langword="null" /> when the value is not a FILETIME.</returns>
    public TimeSpan? AsTimeSpan() =>
        Type == OlePropertyType.FileTime && Value is long ticks ? TimeSpan.FromTicks(ticks) : null;

    /// <summary>
    /// Returns the value as a byte array when it is a blob or clipboard scalar.
    /// </summary>
    /// <returns>The byte array, or <see langword="null" /> when the value is not binary.</returns>
    public byte[]? AsBytes() =>
        Value as byte[];

    /// <summary>
    /// Returns the value as <typeparamref name="T" /> when the boxed value is of that type.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <returns>
    /// The value cast to <typeparamref name="T" />, or the default of <typeparamref name="T" /> when the cast does not
    /// apply.
    /// </returns>
    public T? GetValueOrDefault<T>() =>
        Value is T value ? value : default;

    /// <summary>The largest Windows FILETIME that maps to a representable <see cref="DateTime" />.</summary>
    private static readonly long s_maxFileTime = DateTime.MaxValue.ToFileTimeUtc();
}
