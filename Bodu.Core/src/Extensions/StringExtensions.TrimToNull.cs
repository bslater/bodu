// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.TrimToNull.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the trimmed form of <paramref name="value" />, or <see langword="null" /> when <paramref name="value" />
    /// is <see langword="null" /> or the trimmed result is empty.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>
    /// <see langword="null" /> when <paramref name="value" /> is <see langword="null" /> or trims to an empty string;
    /// otherwise the trimmed value.
    /// </returns>
    /// <remarks>
    /// Useful for input sanitisation pipelines where whitespace-only entries should collapse to a missing-data sentinel
    /// rather than an empty string.
    /// </remarks>
    public static string? TrimToNull(this string? value)
    {
        if (value is null) return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
