// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstCrcTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the behavior of <see cref="PstCrc" />, the MS-PST §5.3 checksum.
/// </summary>
[TestClass]
public class PstCrcTests
{
    /// <summary>
    /// Gets one manifest row per corpus fixture; the header checksum algorithm is identical across variants.
    /// </summary>
    /// <value>One row per fixture.</value>
    public static IEnumerable<object[]> AllFixtureRows =>
        PstReferenceFixtures.Manifest.Fixtures.Select(f => new object[] { f });

    /// <summary>
    /// Verifies the zero-initial-value property of the variant: with no all-ones initialization and no final
    /// complement, empty input and all-zero input both sum to zero.
    /// </summary>
    [TestMethod]
    public void Compute_WhenInputContributesNothing_ShouldReturnZero()
    {
        Assert.AreEqual(0u, PstCrc.Compute(ReadOnlySpan<byte>.Empty));
        Assert.AreEqual(0u, PstCrc.Compute(new byte[16]));
    }

    /// <summary>
    /// Verifies the checksum against the <c>dwCRCPartial</c> every real header records: the sum of the 471 bytes from
    /// <c>wMagicClient</c> must equal the stored value, for each corpus fixture. This pins the §5.3 parameters
    /// (reflected CRC-32, zero initial value, no final exclusive-or) to writer-produced ground truth.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    [TestMethod]
    [DynamicData(
        nameof(AllFixtureRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compute_WhenHeaderPartialRange_ShouldMatchRecordedCrc(PstReferenceFixture fixture)
    {
        byte[] header;
        using (MemoryStream stream = PstReferenceFixtures.OpenStream(fixture.File))
            header = stream.ToArray().AsSpan(0, 512).ToArray();

        uint recorded = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));

        Assert.AreEqual(recorded, PstCrc.Compute(header.AsSpan(8, 471)));
    }
}
