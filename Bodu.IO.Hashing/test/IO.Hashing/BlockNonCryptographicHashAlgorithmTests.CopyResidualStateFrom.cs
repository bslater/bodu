// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithmTests.CopyResidualStateFrom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BlockNonCryptographicHashAlgorithmTests
{

    /// <summary>
    /// Verifies that <see cref="BlockNonCryptographicHashAlgorithm{T}.CopyResidualStateFrom" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> equal to <c>source</c> when invoked with a
    /// <see langword="null" /> source instance.
    /// </summary>
    [TestMethod]
    public void CopyResidualStateFrom_WhenSourceIsNull_ShouldThrowExactly()
    {
        RecordingBlockHasher hasher = new();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => hasher.CopyFromExposed(null));
        Assert.AreEqual("source", ex.ParamName);
    }

}
