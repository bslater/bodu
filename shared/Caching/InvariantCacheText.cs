// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantCacheText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Caching;

/// <summary>
/// Provides the invariant text encodings the text-storing cache backends persist dates and instants with: a
/// <see cref="DateOnly" /> as <c>yyyy-MM-dd</c> and a <see cref="DateTimeOffset" /> in round-trip (<c>"O"</c>) form, so
/// every backend stores the same culture-independent, lexicographically stable representation.
/// </summary>
/// <remarks>
/// This type is a shared cache-infrastructure primitive: it is linked as source into each caching package that needs
/// it (namespace <c>Bodu.Caching</c>) and compiled <see langword="internal" /> per assembly, so no public surface is
/// exposed and no two assemblies collide on the type identity.
/// </remarks>
internal static class InvariantCacheText
{
    /// <summary>
    /// Formats a <see cref="DateOnly" /> as invariant <c>yyyy-MM-dd</c> text for storage.
    /// </summary>
    /// <param name="value">The date to format.</param>
    /// <returns>The invariant ISO date text.</returns>
    public static string FormatDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant <c>yyyy-MM-dd</c> text back into a <see cref="DateOnly" />.
    /// </summary>
    /// <param name="text">The stored date text.</param>
    /// <returns>The parsed date.</returns>
    public static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a <see cref="DateTimeOffset" /> as invariant round-trip (<c>"O"</c>) text for storage.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The invariant round-trip text.</returns>
    public static string FormatInstant(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant round-trip (<c>"O"</c>) text back into a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="text">The stored instant text.</param>
    /// <returns>The parsed instant.</returns>
    public static DateTimeOffset ParseInstant(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>
    /// Formats a <see cref="decimal" /> as invariant text for storage so its scale and precision round-trip losslessly.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant decimal text.</returns>
    public static string FormatDecimal(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant decimal text back into a <see cref="decimal" />.
    /// </summary>
    /// <param name="text">The stored decimal text.</param>
    /// <returns>The parsed value.</returns>
    public static decimal ParseDecimal(string text) =>
        decimal.Parse(text, CultureInfo.InvariantCulture);
}
