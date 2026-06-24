// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReverseComparer{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Comparers;

/// <summary>
/// Provides an <see cref="IComparer{T}" /> that yields the reverse of another comparer's ordering, allowing tests to
/// observe the effect of a non-default comparer on ordering, range, and clamp semantics.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
/// <remarks>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </remarks>
public sealed class ReverseComparer<T>
    : IComparer<T>
{
    private readonly IComparer<T> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReverseComparer{T}" /> class that reverses
    /// <see cref="Comparer{T}.Default" />.
    /// </summary>
    public ReverseComparer()
        : this(Comparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReverseComparer{T}" /> class that reverses the ordering of
    /// <paramref name="inner" />.
    /// </summary>
    /// <param name="inner">The comparer whose ordering is reversed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner" /> is <see langword="null" />.</exception>
    public ReverseComparer(IComparer<T> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Gets a shared instance that reverses <see cref="Comparer{T}.Default" />.
    /// </summary>
    /// <value>
    /// A singleton <see cref="ReverseComparer{T}" /> over the default comparer for <typeparamref name="T" />.
    /// </value>
    public static ReverseComparer<T> Instance { get; } = new();

    /// <inheritdoc />
    public int Compare(T? x, T? y) => _inner.Compare(y, x);
}
