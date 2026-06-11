// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces an <see cref="ObjectConverter{T}" /> for a plain class or struct that no more specific converter handles.
/// This is the catch-all converter, consulted last.
/// </summary>
/// <remarks>
/// The factory deliberately declines primitive and special scalar types — <see cref="decimal" />, enumerations,
/// interfaces, abstract types, and the well-known framework scalar types Bencode cannot natively represent (for example
/// <see cref="DateTime" />, <see cref="Guid" />, <see cref="TimeSpan" />, and <see cref="Uri" />) — so that an
/// unsupported type surfaces as a missing-converter error rather than being mapped to a dictionary of its incidental
/// public properties, which would lose data or recurse on self-referential members. <see cref="object" /> has a
/// dedicated built-in converter earlier in the resolution order, so its rejection here is unreachable through the
/// default list and guards only against a reordering.
/// </remarks>
internal sealed class ObjectConverterFactory
    : BencodeConverterFactory
{
    /// <summary>
    /// The well-known framework scalar types that have no native Bencode mapping. They expose public properties the
    /// object converter would otherwise treat as dictionary entries, so the factory declines them and lets the
    /// serializer report a missing-converter error rather than emitting a lossy dictionary.
    /// </summary>
    private static readonly HashSet<Type> s_unsupportedScalars =
    [
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(Guid),
        typeof(Uri),
        typeof(Version),
        typeof(Half),
    ];

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        if (typeToConvert.IsPrimitive || typeToConvert.IsEnum || typeToConvert.IsPointer || typeToConvert.IsAbstract || typeToConvert.IsArray)
            return false;

        if (typeToConvert == typeof(decimal) || typeToConvert == typeof(object) || typeToConvert == typeof(string))
            return false;

        if (s_unsupportedScalars.Contains(typeToConvert))
            return false;

        if (Nullable.GetUnderlyingType(typeToConvert) is not null)
            return false;

        return typeToConvert.IsClass || typeToConvert.IsValueType;
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(ObjectConverter<>).MakeGenericType(typeToConvert);
        return (BencodeConverter)Activator.CreateInstance(converterType) !;
    }
}
