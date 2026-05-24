// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.Reset.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BKDRTests
{

    /// <summary>
    /// Verifies that <see cref="BKDR.Reset" /> restores the accumulator to the seed so the next empty-input
    /// digest reflects the seed itself, encoded big-endian.
    /// </summary>
    [TestMethod]
    public void Reset_ShouldRestoreAccumulatorToSeed()
    {
        BKDR algorithm = new(13131U);
        algorithm.Append(new byte[] { 0x01, 0x02 });
        algorithm.Reset();

        var actual = algorithm.GetCurrentHash();
        var expected = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(expected, 13131U);

        CollectionAssert.AreEqual(expected, actual);
    }

}
