// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringComparer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Compares bencoded byte strings using raw byte ordinal ordering.
/// </summary>
/// <remarks>
/// <para>
/// The Bencode specification mandates that dictionary keys appear in ascending bytewise order — not any localized or
/// culture-aware collation. <see cref="BencodedStringComparer" /> implements that exact ordering and is used internally
/// by <see cref="BencodedDictionary" /> to enforce it; callers that need to sort a collection of
/// <see cref="BencodedString" /> instances for serialization should reuse the same comparer to stay
/// specification-conformant.
/// </para>
/// <para>
/// The type is stateless; the singleton <see cref="Ordinal" /> is the only instance and is safe to share across
/// threads.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var keys = new[]
/// {
///     BencodedString.FromUtf8("info"),
///     BencodedString.FromUtf8("announce"),
///     BencodedString.FromUtf8("comment"),
/// };
///
/// Array.Sort(keys, BencodedStringComparer.Ordinal);
/// keys is now ordered { announce, comment, info } — bytewise ASCII order.
///]]>
/// </example>
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
