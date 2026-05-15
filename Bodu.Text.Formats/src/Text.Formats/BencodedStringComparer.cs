// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringComparer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Text.Formats;

/// <summary>
/// Compares bencoded byte strings using raw byte ordinal ordering.
/// </summary>
public sealed class BencodedStringComparer
    : IComparer<BencodedString>
    , IEqualityComparer<BencodedString>
{
    /// <summary>
    /// Gets the singleton ordinal byte comparer.
    /// </summary>
    public static BencodedStringComparer Ordinal { get; } = new();

    private BencodedStringComparer()
    {
    }

    /// <inheritdoc />
    public int Compare(BencodedString? x, BencodedString? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        return CompareBytes(x.Bytes.Span, y.Bytes.Span);
    }

    /// <inheritdoc />
    public bool Equals(BencodedString? x, BencodedString? y) =>
        Compare(x, y) == 0;

    /// <inheritdoc />
    public int GetHashCode(BencodedString obj)
    {
        ThrowHelper.ThrowIfNull(obj);

        HashCode hashCode = new();

        foreach (byte value in obj.Bytes.Span)
        {
            hashCode.Add(value);
        }

        return hashCode.ToHashCode();
    }

    internal static int CompareBytes(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        int length = Math.Min(x.Length, y.Length);

        for (int index = 0; index < length; index++)
        {
            int result = x[index].CompareTo(y[index]);

            if (result != 0)
                return result;
        }

        return x.Length.CompareTo(y.Length);
    }
}
