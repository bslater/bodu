// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundNameComparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Compares and orders compound-file directory entry names using the relationship defined by the OLE2 / Compound File
/// Binary format: shorter names sort first, and names of equal length are ordered by their UTF-16 code units after an
/// invariant uppercase conversion.
/// </summary>
/// <remarks>
/// <para>
/// The format treats entry names case-insensitively, so two names that differ only by case denote the same entry. This
/// comparer is the single authority for that relationship and is shared by the reader's child lookups, the writer's
/// red-black directory ordering, and the mutable object model's name keying, ensuring a name that is written one way is
/// found the same way when the container is read back.
/// </para>
/// <para>
/// Equality ignores the length-first ordering rule (names of different lengths are simply unequal) but applies the same
/// per-code-unit uppercase comparison, and the hash code is computed over the uppercased code units so equal names
/// always share a bucket.
/// </para>
/// </remarks>
internal sealed class CompoundNameComparer
    : IComparer<string>, IEqualityComparer<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundNameComparer" /> class.
    /// </summary>
    private CompoundNameComparer()
    {
    }

    /// <summary>
    /// Gets the shared <see cref="CompoundNameComparer" /> instance.
    /// </summary>
    /// <returns>The singleton comparer used for every compound-file name comparison.</returns>
    public static CompoundNameComparer Instance { get; } = new();

    /// <summary>
    /// Compares two entry names using the compound-file ordering: length first, then invariant uppercase per code unit.
    /// </summary>
    /// <param name="x">The first name, or <see langword="null" />.</param>
    /// <param name="y">The second name, or <see langword="null" />.</param>
    /// <returns>
    /// A negative value when <paramref name="x" /> orders before <paramref name="y" />, a positive value when it orders
    /// after, and zero when the names are equal under this relationship. A <see langword="null" /> name orders before a
    /// non-<see langword="null" /> name.
    /// </returns>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        if (x.Length != y.Length)
            return x.Length - y.Length;

        for (int i = 0; i < x.Length; i++)
        {
            int diff = char.ToUpperInvariant(x[i]) - char.ToUpperInvariant(y[i]);
            if (diff != 0)
                return diff;
        }

        return 0;
    }

    /// <summary>
    /// Determines whether two entry names denote the same compound-file entry.
    /// </summary>
    /// <param name="x">The first name, or <see langword="null" />.</param>
    /// <param name="y">The second name, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when the names are equal under the case-insensitive compound-file relationship;
    /// otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null || x.Length != y.Length)
            return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (char.ToUpperInvariant(x[i]) != char.ToUpperInvariant(y[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a hash code for an entry name that is consistent with <see cref="Equals(string, string)" />.
    /// </summary>
    /// <param name="obj">The name to hash.</param>
    /// <returns>A hash code computed over the invariant uppercase code units of <paramref name="obj" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="obj" /> is <see langword="null" />.
    /// </exception>
    public int GetHashCode(string obj)
    {
        ThrowHelper.ThrowIfNull(obj);

        HashCode hash = default;
        hash.Add(obj.Length);
        foreach (char c in obj)
            hash.Add(char.ToUpperInvariant(c));

        return hash.ToHashCode();
    }
}
