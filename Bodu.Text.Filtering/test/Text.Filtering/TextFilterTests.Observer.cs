// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Observer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Cross-cutting tests for the <see cref="ITextFilterObserver" /> hook.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that the observer receives one callback per evaluated value carrying the decision and the deciding
    /// pattern.
    /// </summary>
    [TestMethod]
    public void Observer_WhenAttached_ShouldReceiveEachDecisionWithDecidingPattern()
    {
        var include = TextFilterPattern.Include("e*");
        var exclude = TextFilterPattern.Exclude("*x");
        var filter = TextFilter.Build([include, exclude]);
        var observer = new RecordingObserver();
        filter.Observer = observer;

        _ = filter.IsMatch("e1");
        _ = filter.IsMatch("ex");
        _ = filter.IsMatch("nope");

        Assert.AreEqual(3, observer.Calls.Count);
        Assert.AreEqual(("e1", TextFilterDecision.Included, include), observer.Calls[0]);
        Assert.AreEqual(("ex", TextFilterDecision.Excluded, exclude), observer.Calls[1]);
        Assert.AreEqual(("nope", TextFilterDecision.NotIncluded, (TextFilterPattern?)null), observer.Calls[2]);
    }

    /// <summary>
    /// Verifies that detaching the observer stops the callbacks.
    /// </summary>
    [TestMethod]
    public void Observer_WhenDetached_ShouldStopReceivingCallbacks()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*")]);
        var observer = new RecordingObserver();
        filter.Observer = observer;
        _ = filter.IsMatch("e1");

        filter.Observer = null;
        _ = filter.IsMatch("e2");

        Assert.AreEqual(1, observer.Calls.Count);
    }

    /// <summary>
    /// Verifies that the observer also sees values evaluated through the sequence-filtering surface.
    /// </summary>
    [TestMethod]
    public void Observer_WhenFilteringSequence_ShouldSeeEveryEvaluatedValue()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*")]);
        var observer = new RecordingObserver();
        filter.Observer = observer;

        _ = filter.Filter(["e1", "x2", "e3"]).ToArray();

        CollectionAssert.AreEqual(new[] { "e1", "x2", "e3" }, observer.Calls.Select(c => c.Value).ToArray());
    }

    /// <summary>
    /// Verifies that an exception thrown by the observer propagates to the evaluating caller.
    /// </summary>
    [TestMethod]
    public void Observer_WhenCallbackThrows_ShouldPropagateToCaller()
    {
        var filter = TextFilter.Build([]);
        filter.Observer = new ThrowingObserver();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = filter.IsMatch("anything");
        });
    }

    /// <summary>
    /// An observer test double whose callback always throws.
    /// </summary>
    private sealed class ThrowingObserver : ITextFilterObserver
    {
        /// <inheritdoc />
        public void OnEvaluated(ReadOnlySpan<char> value, TextFilterDecision decision, TextFilterPattern? pattern) =>
            throw new InvalidOperationException("Observer failure.");
    }
}
