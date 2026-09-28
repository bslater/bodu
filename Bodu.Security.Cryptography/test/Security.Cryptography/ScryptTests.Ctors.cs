// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ScryptTests
{
    /// <summary>
    /// Verifies that the parameters constructor with a bound rejects a bound of zero or below <c>-1</c>, naming the
    /// parameter.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The invalid bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    [DataRow(int.MinValue)]
    public void Constructor_WhenBoundIsInvalid_ForParameters_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        var parameters = new ScryptParameters { CostN = 16, BlockSizeR = 1, Parallelization = 1 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Scrypt(parameters, maxDegreeOfParallelism);
        });

        Assert.AreEqual(nameof(maxDegreeOfParallelism), ex.ParamName);
    }

    /// <summary>
    /// Verifies that the cost-parameter constructor with a bound rejects a bound of zero or below <c>-1</c>, naming the
    /// parameter.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The invalid bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void Constructor_WhenBoundIsInvalid_ForCostParameters_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Scrypt(16, 1, 1, maxDegreeOfParallelism);
        });

        Assert.AreEqual(nameof(maxDegreeOfParallelism), ex.ParamName);
    }

    /// <summary>
    /// Verifies that the parameters constructor with a bound rejects <see langword="null" /> parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenParametersAreNull_ForBound_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Scrypt(null!, 2);
        });

        Assert.AreEqual("parameters", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the cost-parameter constructor with a bound still validates the cost parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCostNotPowerOfTwo_ForBound_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Scrypt(1000, 8, 1, 4);
        });

        Assert.AreEqual("CostN", ex.ParamName);
    }
}
