// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TriePrefixKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents a known-answer scenario for trie prefix queries: a set of keys to insert, a query prefix, and the keys
/// expected to match.
/// </summary>
/// <param name="Name">The human-readable scenario name surfaced on test failure.</param>
/// <param name="Keys">The keys to insert into the trie.</param>
/// <param name="Prefix">The prefix to query.</param>
/// <param name="Expected">The keys expected to begin with <paramref name="Prefix" />, in any order.</param>
/// <param name="OrdinalIgnoreCase">Whether the trie should use case-insensitive character comparison.</param>
public sealed record TriePrefixKat(
    string Name,
    string[] Keys,
    string Prefix,
    string[] Expected,
    bool OrdinalIgnoreCase = false) : IKat;
