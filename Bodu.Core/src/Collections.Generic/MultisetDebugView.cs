// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="Multiset{T}" /> contents, showing each distinct element
/// alongside its occurrence count.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the multiset.</typeparam>
internal sealed class MultisetDebugView<T>
    where T : notnull
{
    /// <summary>
    /// The <see cref="Multiset{T}" /> instance being debugged.
    /// </summary>
    private readonly Multiset<T> _multiset;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultisetDebugView{T}" /> class.
    /// </summary>
    /// <param name="multiset">The multiset instance to inspect. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="multiset" /> is <see langword="null" />.</exception>
    public MultisetDebugView(Multiset<T> multiset)
    {
        _multiset = multiset ?? throw new ArgumentNullException(nameof(multiset));
    }

    /// <summary>
    /// Gets the element–count pairs currently held in the <see cref="Multiset{T}" />, displayed in the debugger as
    /// root-level entries.
    /// </summary>
    /// <remarks>
    /// Shown in the debugger as root-level entries for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<T, int>[] Frequencies =>
        [.. _multiset.Frequencies()];
}
