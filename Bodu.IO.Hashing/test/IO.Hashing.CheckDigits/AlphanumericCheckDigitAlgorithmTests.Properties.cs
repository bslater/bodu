// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the algorithm name, input alphabet, and output
    /// alphabet declared in the specification.
    /// </summary>
    [TestMethod]
    public void Properties_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.AlgorithmName, algorithm.AlgorithmName);
        Assert.AreEqual(spec.InputAlphabet, algorithm.InputAlphabet);
        Assert.AreEqual(spec.OutputAlphabet, algorithm.OutputAlphabet);
    }
}
