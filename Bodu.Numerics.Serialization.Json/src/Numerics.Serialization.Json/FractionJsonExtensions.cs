// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Convenience JSON helpers for <see cref="Fraction{T}" /> that serialize through the <c>Bodu.Numerics</c> converters
/// under a chosen <see cref="NumericsJsonPolicy" />.
/// </summary>
/// <remarks>
/// These helpers moved here from the core library so that <c>Bodu.Numerics</c> takes no dependency on
/// <see cref="System.Text.Json" />. Each call configures a fresh <see cref="JsonSerializerOptions" /> and registers the
/// numeric converters; for repeated serialization prefer building one <see cref="JsonSerializerOptions" /> with
/// <see cref="NumericsJsonSerializerOptionsExtensions.ConfigureForBoduNumerics" /> and reusing it.
/// </remarks>
public static class FractionJsonExtensions
{
    /// <summary>
    /// Serializes this rational value to JSON under the supplied <paramref name="policy" />.
    /// </summary>
    /// <typeparam name="T">The backing integer type of the fraction.</typeparam>
    /// <param name="value">The rational value to serialize.</param>
    /// <param name="policy">The serialization shape to apply. Defaults to <see cref="NumericsJsonPolicy.Strict" />.</param>
    /// <returns>The JSON text representing <paramref name="value" />.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization of Fraction<T> uses the reflection-based JsonSerializer. Use a source-generated JsonSerializerContext for trimming and AOT.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization of Fraction<T> uses the reflection-based JsonSerializer. Use a source-generated JsonSerializerContext for trimming and AOT.")]
    public static string ToJson<T>(this Fraction<T> value, NumericsJsonPolicy policy = NumericsJsonPolicy.Strict)
        where T : IBinaryInteger<T>
    {
        var options = new JsonSerializerOptions().ConfigureForBoduNumerics(policy);
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a <see cref="Fraction{T}" /> from its JSON representation under the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <typeparam name="T">The backing integer type of the fraction.</typeparam>
    /// <param name="json">The JSON text to deserialize.</param>
    /// <param name="policy">The serialization shape to expect. Defaults to <see cref="NumericsJsonPolicy.Strict" />.</param>
    /// <returns>The rational value the JSON text represents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is <see langword="null" />.</exception>
    /// <exception cref="JsonException"><paramref name="json" /> is not a valid fraction under <paramref name="policy" />.</exception>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON deserialization of Fraction<T> uses the reflection-based JsonSerializer. Use a source-generated JsonSerializerContext for trimming and AOT.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON deserialization of Fraction<T> uses the reflection-based JsonSerializer. Use a source-generated JsonSerializerContext for trimming and AOT.")]
    public static Fraction<T> FromJson<T>(string json, NumericsJsonPolicy policy = NumericsJsonPolicy.Strict)
        where T : IBinaryInteger<T>
    {
        ThrowHelper.ThrowIfNull(json);

        var options = new JsonSerializerOptions().ConfigureForBoduNumerics(policy);
        return JsonSerializer.Deserialize<Fraction<T>>(json, options);
    }
}
