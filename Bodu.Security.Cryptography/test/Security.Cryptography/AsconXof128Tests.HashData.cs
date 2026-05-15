// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.HashData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.HashData(ReadOnlySpan{byte}, int)" /> with
    /// <c>outputLength = 0</c> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void HashData_WhenOutputLengthIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = AsconXof128.HashData([0x01], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.HashData(ReadOnlySpan{byte}, int)" /> with a negative
    /// <c>outputLength</c> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void HashData_WhenOutputLengthIsNegative_ShouldThrowExactly(int outputLength)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = AsconXof128.HashData([0x01], outputLength);
        });
    }
}
