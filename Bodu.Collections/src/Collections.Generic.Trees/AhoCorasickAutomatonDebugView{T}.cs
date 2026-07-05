// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of an <see cref="AhoCorasickAutomaton{TValue}" />, rendering its pattern/value
/// pairs as a flat array.
/// </summary>
/// <typeparam name="TValue">The type of the value associated with each pattern.</typeparam>
internal sealed class AhoCorasickAutomatonDebugView<TValue>
{
    /// <summary>The automaton whose pattern/value pairs are surfaced in the debugger.</summary>
    private readonly AhoCorasickAutomaton<TValue> _automaton;

    /// <summary>
    /// Initializes a new instance of the <see cref="AhoCorasickAutomatonDebugView{TValue}" /> class.
    /// </summary>
    /// <param name="automaton">The automaton to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="automaton" /> is <see langword="null" />.</exception>
    public AhoCorasickAutomatonDebugView(AhoCorasickAutomaton<TValue> automaton)
    {
        _automaton = automaton ?? throw new ArgumentNullException(nameof(automaton));
    }

    /// <summary>
    /// Gets a snapshot of the automaton's pattern/value pairs.
    /// </summary>
    /// <value>An array of the automaton's patterns and their associated values, in supplied order.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<string, TValue>[] Items => _automaton.ToArrayInternal();
}
