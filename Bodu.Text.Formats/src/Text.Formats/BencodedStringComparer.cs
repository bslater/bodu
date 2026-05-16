// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringComparer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Compares bencoded byte strings using raw byte ordinal ordering.
/// </summary>
public sealed class BencodedStringComparer
    : IComparer<BencodedString>
    , IEqualityComparer<BencodedString>
{
    private BencodedStringComparer()
    {
    }

    /// <summary>
    /// Gets the singleton ordinal byte comparer.
    /// </summary>
    public static BencodedStringComparer Ordinal { get; } = new();

    /// <inheritdoc />
    public int Compare(BencodedString? x, BencodedString? y) =>
        ReferenceEquals(x, y) ? 0 : x is null ? -1 : y is null ? 1 : CompareBytes(x.Bytes.Span, y.Bytes.Span);

    /// <inheritdoc />
    public bool Equals(BencodedString? x, BencodedString? y) =>
        Compare(x, y) == 0;

    /// <inheritdoc />
    public int GetHashCode(BencodedString obj)
    {
        ThrowHelper.ThrowIfNull(obj);

        HashCode hashCode = new();

        foreach (var value in obj.Bytes.Span)
        {
            hashCode.Add(value);
        }

        return hashCode.ToHashCode();
    }

    internal static int CompareBytes(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        var length = Math.Min(x.Length, y.Length);

        for (var index = 0; index < length; index++)
        {
            var result = x[index].CompareTo(y[index]);

            if (result != 0)
                return result;
        }

        return x.Length.CompareTo(y.Length);
    }
}
