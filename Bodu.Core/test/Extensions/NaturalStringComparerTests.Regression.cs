// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Regression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// A natsort-style corpus — IP addresses, version-ish strings, zero-padded counters, and long digit runs — listed
    /// in the exact order <see cref="NaturalStringComparer.Ordinal" /> is expected to produce.
    /// </summary>
    private static readonly string[] NaturalOrderCorpus =
    [
        "0",
        "00",
        "1",
        "01",
        "001",
        "1.0.0",
        "1.0.9",
        "1.0.10",
        "1.2.0",
        "1.10.0",
        "2",
        "9",
        "10",
        "10.0.0.1",
        "10.0.0.2",
        "10.0.0.02",
        "10.0.0.10",
        "10.0.2.1",
        "10.0.10.1",
        "99",
        "100",
        "192.168.0.1",
        "192.168.0.2",
        "192.168.0.10",
        "192.168.0.100",
        "192.168.1.1",
        "999999999999999999999999999999",
        "1000000000000000000000000000000",
        "a",
        "a0",
        "a1",
        "a1b",
        "a1b2",
        "a2",
        "a02",
        "a10",
        "a10b9",
        "a10b10",
        "b",
        "file",
        "file1",
        "file2",
        "file07",
        "file7z",
        "file10",
        "v1.0",
        "v1.2",
        "v1.10",
        "v2.0",
        "z1",
        "z2",
        "z10",
        "z11",
    ];

    /// <summary>
    /// Verifies that sorting a deterministically shuffled copy of the natsort-style corpus with
    /// <see cref="NaturalStringComparer.Ordinal" /> reproduces the full expected order, covering IP addresses,
    /// version-ish strings, mixed zero padding, and overflow-length digit runs.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Compare_WhenSortingShuffledCorpus_ShouldReproduceFullNaturalOrder()
    {
        var shuffled = (string[])NaturalOrderCorpus.Clone();
        var random = new Random(20260705);

        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        Array.Sort(shuffled, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(NaturalOrderCorpus, shuffled);
    }

    /// <summary>
    /// Verifies that sorting the corpus in fully reversed order with <see cref="NaturalStringComparer.Ordinal" />
    /// also reproduces the expected order, exercising every adjacent pair in the opposite direction.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Compare_WhenSortingReversedCorpus_ShouldReproduceFullNaturalOrder()
    {
        var reversed = (string[])NaturalOrderCorpus.Clone();
        Array.Reverse(reversed);

        Array.Sort(reversed, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(NaturalOrderCorpus, reversed);
    }
}
