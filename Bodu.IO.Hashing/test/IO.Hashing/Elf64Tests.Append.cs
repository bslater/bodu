// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class Elf64Tests
{
    /// <summary>
    /// Verifies that different seed values produce distinct digests for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WhenSeedDiffers_ShouldProduceDifferentHash()
    {
        byte[] input = [0x10, 0x20, 0x30];

        Elf64 a = new(seed: 0UL);
        Elf64 b = new(seed: 131UL);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }
}
