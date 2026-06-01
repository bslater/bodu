// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateDateSearchTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

[TestClass]
public class ExchangeRateDateSearchTests
{
    private static readonly int[] s_dayNumbers = { 1000, 1010, 1020 };

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.PreviousOnOrBefore" /> selects the previous index when
    /// one exists.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenPreviousOnOrBeforeAndPreviousAvailable_ShouldSelectPrevious()
    {
        var requested = 1005;
        var previous = 0;
        var next = 1;

        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, ExchangeRateDateResolution.PreviousOnOrBefore, previous, next, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.PreviousOnOrBefore" /> reports failure when the
    /// requested day precedes every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenPreviousOnOrBeforeAndNoPrevious_ShouldFail()
    {
        var requested = 999;
        var previous = -1;
        var next = 0;

        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, ExchangeRateDateResolution.PreviousOnOrBefore, previous, next, out var candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.NextOnOrAfter" /> selects the next index when one
    /// exists.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNextOnOrAfterAndNextAvailable_ShouldSelectNext()
    {
        var requested = 1005;
        var previous = 0;
        var next = 1;

        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, ExchangeRateDateResolution.NextOnOrAfter, previous, next, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.NextOnOrAfter" /> reports failure when the requested day
    /// follows every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNextOnOrAfterAndNoNext_ShouldFail()
    {
        var requested = 1100;
        var previous = 2;
        var next = 3; // == dayNumbers.Length

        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, ExchangeRateDateResolution.NextOnOrAfter, previous, next, out var candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Nearest" /> selects the closer of the previous and next
    /// candidates when they are unequally distant.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndPreviousCloser_ShouldSelectPrevious()
    {
        // requested 1003 — previous 1000 distance 3, next 1010 distance 7
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1003, ExchangeRateDateResolution.Nearest, 0, 1, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Nearest" /> selects the next candidate when it is
    /// closer to the requested date.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndNextCloser_ShouldSelectNext()
    {
        // requested 1008 — previous 1000 distance 8, next 1010 distance 2
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1008, ExchangeRateDateResolution.Nearest, 0, 1, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Nearest" /> with a tie returns <see langword="false" />
    /// because the policy cannot deterministically pick a side.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndTie_ShouldFail()
    {
        // requested 1005 — previous 1000 distance 5, next 1010 distance 5
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, ExchangeRateDateResolution.Nearest, 0, 1, out var candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.NearestPreferPrevious" /> selects the previous
    /// candidate on a tie.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestPreferPreviousAndTie_ShouldSelectPrevious()
    {
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, ExchangeRateDateResolution.NearestPreferPrevious, 0, 1, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.NearestPreferNext" /> selects the next candidate on a
    /// tie.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestPreferNextAndTie_ShouldSelectNext()
    {
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, ExchangeRateDateResolution.NearestPreferNext, 0, 1, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Nearest" /> falls back to the only available candidate
    /// when the requested day precedes every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndOnlyNextAvailable_ShouldSelectNext()
    {
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 999, ExchangeRateDateResolution.Nearest, -1, 0, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Nearest" /> falls back to the only available candidate
    /// when the requested day follows every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndOnlyPreviousAvailable_ShouldSelectPrevious()
    {
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1100, ExchangeRateDateResolution.Nearest, 2, 3, out var candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(2, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateDateResolution.Exact" /> always returns <see langword="false" />
    /// because the caller is responsible for the exact-hit fast path.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenExact_ShouldAlwaysFail()
    {
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, ExchangeRateDateResolution.Exact, 0, 1, out var candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that when neither a previous nor a next candidate exists (only possible against an empty
    /// day-number span) the helper reports failure.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNoCandidatesAvailable_ShouldFail()
    {
        int[] empty = Array.Empty<int>();
        var found = ExchangeRateDateSearch.TrySelectCandidate(
            empty, 1000, ExchangeRateDateResolution.Nearest, -1, 0, out var candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }
}
