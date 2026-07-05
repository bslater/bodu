// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomaton{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents an immutable Aho-Corasick automaton that locates every occurrence of every pattern in a keyed pattern set
/// in a single pass over the searched text, reporting the value associated with each matched pattern.
/// </summary>
/// <typeparam name="TValue">The type of the value associated with each pattern.</typeparam>
/// <remarks>
/// <para>
/// The automaton is built once from a complete pattern set via
/// <see cref="Build(IEnumerable{KeyValuePair{string, TValue}})" /> and is immutable thereafter: its failure and output
/// links are global invariants defined relative to the whole pattern set, so a pattern added later would invalidate the
/// links wholesale — rebuild with the new set instead. Matching costs O(n + m) for text length n and m reported
/// matches, independent of the number of patterns.
/// </para>
/// <para>
/// Character comparison is ordinal. Matches — including overlapping and nested occurrences — are reported in a
/// deterministic order: ascending end index, then ascending pattern length. Because a lazily evaluated sequence cannot
/// capture a <see cref="ReadOnlySpan{Char}" />, the lazy <see cref="EnumerateMatches(string)" /> takes a
/// <see cref="string" />, while the eager <see cref="CountMatches(ReadOnlySpan{char})" /> and
/// <see cref="HasMatch(ReadOnlySpan{char})" /> conveniences accept spans. For the unkeyed variant, see
/// <see cref="AhoCorasickAutomaton" />.
/// </para>
/// </remarks>
[DebuggerDisplay("Patterns = {Patterns.Count}")]
[DebuggerTypeProxy(typeof(AhoCorasickAutomatonDebugView<>))]
public sealed class AhoCorasickAutomaton<TValue>
{
    /// <summary>The root state of the automaton; the scan starts and recovers here.</summary>
    private readonly AhoCorasickNode<TValue> _root;

    /// <summary>The patterns, in the order supplied to <see cref="Build(IEnumerable{KeyValuePair{string, TValue}})" />.</summary>
    private readonly ReadOnlyCollection<string> _patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="AhoCorasickAutomaton{TValue}" /> class over a fully linked state
    /// machine.
    /// </summary>
    /// <param name="root">The root state with goto, failure, and output links already built.</param>
    /// <param name="patterns">The patterns, in supplied order.</param>
    private AhoCorasickAutomaton(AhoCorasickNode<TValue> root, ReadOnlyCollection<string> patterns)
    {
        _root = root;
        _patterns = patterns;
    }

    /// <summary>
    /// Gets the patterns the automaton searches for, in the order they were supplied.
    /// </summary>
    /// <value>A read-only collection of the patterns.</value>
    public IReadOnlyCollection<string> Patterns => _patterns;

    /// <summary>
    /// Builds an automaton that searches for every pattern key in the specified set, associating each with its value.
    /// </summary>
    /// <param name="patterns">The pattern/value pairs to search for.</param>
    /// <returns>An immutable automaton over the pattern set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patterns" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="patterns" /> is empty, contains a <see langword="null" /> pattern key, contains an empty pattern
    /// key, or contains a duplicate pattern key.
    /// </exception>
    /// <remarks>
    /// Unlike the unkeyed <see cref="AhoCorasickAutomaton.Build(IEnumerable{string})" />, a duplicate pattern key
    /// throws rather than deduplicating, because two entries would compete for the key's value — mirroring the
    /// <see cref="Trie{TValue}" /> versus <see cref="Trie" /> add contracts.
    /// </remarks>
    public static AhoCorasickAutomaton<TValue> Build(IEnumerable<KeyValuePair<string, TValue>> patterns)
    {
        ThrowHelper.ThrowIfNull(patterns);

        var root = new AhoCorasickNode<TValue>();
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TValue> pair in patterns)
        {
            if (pair.Key is null) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_NullCollectionElement, nameof(patterns));
            if (pair.Key.Length == 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_EmptyPattern, nameof(patterns));
            if (!seen.Add(pair.Key)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(patterns));

            ordered.Add(pair.Key);
            AhoCorasickNode<TValue> node = AhoCorasickCore.AddPattern(root, pair.Key);
            node.Pattern = pair.Key;
            node.Value = pair.Value;
        }

        if (ordered.Count == 0) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_CollectionIsEmpty, nameof(patterns));

        AhoCorasickCore.BuildLinks(root);
        return new AhoCorasickAutomaton<TValue>(root, ordered.AsReadOnly());
    }

    /// <summary>
    /// Determines whether the automaton was built with the specified pattern.
    /// </summary>
    /// <param name="pattern">The pattern to locate.</param>
    /// <returns><see langword="true" /> if the pattern is in the set; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
    public bool ContainsPattern(string pattern)
    {
        ThrowHelper.ThrowIfNull(pattern);

        return AhoCorasickCore.Find(_root, pattern.AsSpan())?.Pattern is not null;
    }

    /// <summary>
    /// Returns every occurrence of every pattern in the specified text, carrying each pattern's associated value.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>
    /// A lazily evaluated sequence of matches — overlapping and nested occurrences included — ordered ascending by end
    /// index, then ascending by pattern length.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    public IEnumerable<AhoCorasickMatch<TValue>> EnumerateMatches(string text)
    {
        ThrowHelper.ThrowIfNull(text);

        return EnumerateMatchesCore(text);
    }

    /// <summary>
    /// Counts every occurrence of every pattern in the specified text.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>The total number of matches, overlapping and nested occurrences included.</returns>
    /// <remarks>
    /// This is the eager span-friendly companion of <see cref="EnumerateMatches(string)" />; it reports the same number
    /// of matches without materializing them.
    /// </remarks>
    public int CountMatches(ReadOnlySpan<char> text) =>
        AhoCorasickCore.CountMatches(_root, text);

    /// <summary>
    /// Determines whether any pattern occurs in the specified text.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns><see langword="true" /> if at least one pattern occurs; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// The scan exits at the first hit, so a match near the start of the text returns immediately.
    /// </remarks>
    public bool HasMatch(ReadOnlySpan<char> text) =>
        AhoCorasickCore.HasMatch(_root, text);

    /// <summary>
    /// Builds a point-in-time array of the pattern/value pairs, used by the debugger proxy.
    /// </summary>
    /// <returns>An array containing every pattern and its associated value, in supplied order.</returns>
    internal KeyValuePair<string, TValue>[] ToArrayInternal()
    {
        var result = new KeyValuePair<string, TValue>[_patterns.Count];
        for (int i = 0; i < _patterns.Count; i++)
        {
            AhoCorasickNode<TValue> node = AhoCorasickCore.Find(_root, _patterns[i].AsSpan()) !;
            result[i] = new KeyValuePair<string, TValue>(_patterns[i], node.Value);
        }

        return result;
    }

    /// <summary>
    /// Scans the text and yields matches in the documented (end index, pattern length) order.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <returns>A lazy sequence of matches.</returns>
    private IEnumerable<AhoCorasickMatch<TValue>> EnumerateMatchesCore(string text)
    {
        var buffer = new List<AhoCorasickNode<TValue>>();
        AhoCorasickNode<TValue> state = _root;
        for (int i = 0; i < text.Length; i++)
        {
            state = AhoCorasickCore.Step(_root, state, text[i]);
            if (AhoCorasickCore.FirstTerminal(state) is null)
                continue;

            AhoCorasickCore.CollectTerminalsAscending(state, buffer);
            foreach (AhoCorasickNode<TValue> terminal in buffer)
                yield return new AhoCorasickMatch<TValue>(terminal.Pattern!, i - terminal.Pattern!.Length + 1, terminal.Value);
        }
    }
}
