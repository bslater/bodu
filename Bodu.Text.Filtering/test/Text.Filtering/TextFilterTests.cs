// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Tests for <see cref="TextFilter" />.
/// </summary>
[TestClass]
public partial class TextFilterTests
{
    /// <summary>
    /// An observer test double that records every callback it receives.
    /// </summary>
    private sealed class RecordingObserver : ITextFilterObserver
    {
        /// <summary>
        /// Gets the recorded callbacks: the evaluated value, the decision, and the deciding pattern.
        /// </summary>
        public List<(string Value, TextFilterDecision Decision, TextFilterPattern? Pattern)> Calls { get; } = [];

        /// <inheritdoc />
        public void OnEvaluated(ReadOnlySpan<char> value, TextFilterDecision decision, TextFilterPattern? pattern) =>
            Calls.Add((value.ToString(), decision, pattern));
    }
}
