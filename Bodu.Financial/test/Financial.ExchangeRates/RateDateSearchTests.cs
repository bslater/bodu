// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateDateSearchTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public class RateDateSearchTests
{
    private static readonly int[] s_dayNumbers = [1000, 1010, 1020];

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.PreviousOnOrBefore" /> selects the previous index when
    /// one exists.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenPreviousOnOrBeforeAndPreviousAvailable_ShouldSelectPrevious()
    {
        int requested = 1005;
        int previous = 0;
        int next = 1;

        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, RateDateResolution.PreviousOnOrBefore, previous, next, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.PreviousOnOrBefore" /> reports failure when the
    /// requested day precedes every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenPreviousOnOrBeforeAndNoPrevious_ShouldFail()
    {
        int requested = 999;
        int previous = -1;
        int next = 0;

        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, RateDateResolution.PreviousOnOrBefore, previous, next, out int candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.NextOnOrAfter" /> selects the next index when one
    /// exists.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNextOnOrAfterAndNextAvailable_ShouldSelectNext()
    {
        int requested = 1005;
        int previous = 0;
        int next = 1;

        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, RateDateResolution.NextOnOrAfter, previous, next, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.NextOnOrAfter" /> reports failure when the requested day
    /// follows every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNextOnOrAfterAndNoNext_ShouldFail()
    {
        int requested = 1100;
        int previous = 2;
        int next = 3; // == dayNumbers.Length

        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, requested, RateDateResolution.NextOnOrAfter, previous, next, out int candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Nearest" /> selects the closer of the previous and next
    /// candidates when they are unequally distant.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndPreviousCloser_ShouldSelectPrevious()
    {
        // requested 1003 — previous 1000 distance 3, next 1010 distance 7
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1003, RateDateResolution.Nearest, 0, 1, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Nearest" /> selects the next candidate when it is
    /// closer to the requested date.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndNextCloser_ShouldSelectNext()
    {
        // requested 1008 — previous 1000 distance 8, next 1010 distance 2
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1008, RateDateResolution.Nearest, 0, 1, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Nearest" /> with a tie returns <see langword="false" />
    /// because the policy cannot deterministically pick a side.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndTie_ShouldFail()
    {
        // requested 1005 — previous 1000 distance 5, next 1010 distance 5
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, RateDateResolution.Nearest, 0, 1, out int candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.NearestPreferPrevious" /> selects the previous
    /// candidate on a tie.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestPreferPreviousAndTie_ShouldSelectPrevious()
    {
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, RateDateResolution.NearestPreferPrevious, 0, 1, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.NearestPreferNext" /> selects the next candidate on a
    /// tie.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestPreferNextAndTie_ShouldSelectNext()
    {
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, RateDateResolution.NearestPreferNext, 0, 1, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(1, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Nearest" /> falls back to the only available candidate
    /// when the requested day precedes every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndOnlyNextAvailable_ShouldSelectNext()
    {
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 999, RateDateResolution.Nearest, -1, 0, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(0, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Nearest" /> falls back to the only available candidate
    /// when the requested day follows every observation.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenNearestAndOnlyPreviousAvailable_ShouldSelectPrevious()
    {
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1100, RateDateResolution.Nearest, 2, 3, out int candidate);

        Assert.IsTrue(found);
        Assert.AreEqual(2, candidate);
    }

    /// <summary>
    /// Verifies that <see cref="RateDateResolution.Exact" /> always returns <see langword="false" />
    /// because the caller is responsible for the exact-hit fast path.
    /// </summary>
    [TestMethod]
    public void TrySelectCandidate_WhenExact_ShouldAlwaysFail()
    {
        bool found = RateDateSearch.TrySelectCandidate(
            s_dayNumbers, 1005, RateDateResolution.Exact, 0, 1, out int candidate);

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
        bool found = RateDateSearch.TrySelectCandidate(
            empty, 1000, RateDateResolution.Nearest, -1, 0, out int candidate);

        Assert.IsFalse(found);
        Assert.AreEqual(-1, candidate);
    }
}
