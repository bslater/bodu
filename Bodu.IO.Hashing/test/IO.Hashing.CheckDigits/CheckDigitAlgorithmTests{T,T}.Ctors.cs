// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests{T,T}.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the algorithm name declared in the specification.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        CheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.AlgorithmName, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm reports the empty-body check digit declared in the
    /// specification.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenJustConstructed_ShouldReturnEmptyCheckDigit()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        CheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.EmptyCheckDigit, algorithm.GetCurrentCheckDigit());
    }

}
