// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.Properties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the algorithm name declared in the specification.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.AlgorithmName, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the check length declared in the specification.
    /// </summary>
    [TestMethod]
    public void CheckLength_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.CheckLength, algorithm.CheckLength);
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the input alphabet declared in the specification.
    /// </summary>
    [TestMethod]
    public void InputAlphabet_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.InputAlphabet, algorithm.InputAlphabet);
    }

}
