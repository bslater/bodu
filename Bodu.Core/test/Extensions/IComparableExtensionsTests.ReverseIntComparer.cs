// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.ReverseIntComparer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Provides an <see cref="IComparer{T}" /> whose ordering is the reverse of the natural integer comparer,
    /// allowing tests to observe the effect of a custom comparer on range and clamp semantics.
    /// </summary>
    internal sealed class ReverseIntComparer
        : IComparer<int>
    {

        public static readonly ReverseIntComparer Instance = new();

        public int Compare(int x, int y) => y.CompareTo(x);

    }

}
