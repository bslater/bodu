// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class Elf64Tests
{
    /// <summary>
    /// Verifies that <see cref="Elf64.Reset" /> restores the accumulator to the seed so the next empty-input
    /// digest reflects the seed itself, encoded big-endian.
    /// </summary>
    [TestMethod]
    public void Reset_ShouldRestoreAccumulatorToSeed()
    {
        Elf64 algorithm = new(131UL);
        algorithm.Append(new byte[] { 0x01, 0x02 });
        algorithm.Reset();

        byte[] actual = algorithm.GetCurrentHash();
        byte[] expected = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(expected, 131UL);

        CollectionAssert.AreEqual(expected, actual);
    }
}
