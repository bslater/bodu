// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Produces an <see cref="ObjectConverter{T}" /> for a plain class or struct that no more specific converter handles.
/// This is the catch-all converter, consulted last.
/// </summary>
/// <remarks>
/// The factory deliberately declines primitive and special scalar types — <see cref="decimal" />, enumerations,
/// interfaces, abstract types, and the like — so that an unsupported type surfaces as a missing-converter error rather
/// than being mapped to an empty object.
/// </remarks>
internal sealed class ObjectConverterFactory
    : FormatConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        if (typeToConvert.IsPrimitive || typeToConvert.IsEnum || typeToConvert.IsPointer || typeToConvert.IsAbstract || typeToConvert.IsArray)
            return false;

        if (typeToConvert == typeof(decimal) || typeToConvert == typeof(object) || typeToConvert == typeof(string))
            return false;

        if (Nullable.GetUnderlyingType(typeToConvert) is not null)
            return false;

        return typeToConvert.IsClass || typeToConvert.IsValueType;
    }

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(ObjectConverter<>).MakeGenericType(typeToConvert);
        return (FormatConverter)Activator.CreateInstance(converterType) !;
    }
}
