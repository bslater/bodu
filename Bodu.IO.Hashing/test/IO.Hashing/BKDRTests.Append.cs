// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BKDRTests
{

    /// <summary>
    /// Verifies that different seed values produce distinct digests for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WhenSeedDiffers_ShouldProduceDifferentHash()
    {
        byte[] input = [0x10, 0x20, 0x30];

        BKDR a = new(131313U);
        BKDR b = new(13131U);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

}
