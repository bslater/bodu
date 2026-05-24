// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.128.HashLengthInBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class CityHash128Tests
{

    /// <summary>
    /// Verifies that a <see cref="CityHash128" /> instance reports a 16-byte hash length.
    /// </summary>
    [TestMethod]
    public void HashLengthInBytes_ShouldBeSixteen()
    {
        CityHash128 algorithm = CreateAlgorithm();
        Assert.AreEqual(16, algorithm.HashLengthInBytes);
    }

}
