// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.UseModifiedAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

public partial class BernsteinTests
{

    /// <summary>
    /// Verifies that <see cref="Bernstein.UseModifiedAlgorithm" /> defaults to <see langword="false" /> on a
    /// freshly constructed instance.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenConstructedWithDefault_ShouldReturnFalse()
    {
        Bernstein algorithm = new();

        Assert.IsFalse(algorithm.UseModifiedAlgorithm);
    }

    /// <summary>
    /// Verifies that <see cref="Bernstein.UseModifiedAlgorithm" /> returns the value supplied to the constructor.
    /// </summary>
    /// <param name="useModified">The configuration flag under test.</param>
    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void UseModifiedAlgorithm_WhenConstructorSuppliesValue_ShouldExposeSameValue(bool useModified)
    {
        Bernstein algorithm = new(initialValue: Bernstein.DefaultInitialValue, useModifiedAlgorithm: useModified);

        Assert.AreEqual(useModified, algorithm.UseModifiedAlgorithm);
    }

    /// <summary>
    /// Verifies that setting <see cref="Bernstein.UseModifiedAlgorithm" /> after input has been consumed
    /// throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSetAfterHashingStarted_ShouldThrowExactly()
    {
        Bernstein algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.UseModifiedAlgorithm = true);
    }
    /// <summary>
    /// Verifies that toggling <see cref="Bernstein.UseModifiedAlgorithm" /> before any input has been consumed
    /// switches the active mixing form.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSetBeforeHashing_ShouldAffectResult()
    {
        var input = NonCryptographicHashSharedInputs.Abc;

        Bernstein original = new();
        Bernstein modified = new() { UseModifiedAlgorithm = true };
        original.Append(input);
        modified.Append(input);

        CollectionAssert.AreNotEqual(original.GetCurrentHash(), modified.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that toggling <see cref="Bernstein.UseModifiedAlgorithm" /> before any input updates the property
    /// getter so the new value is observable.
    /// </summary>
    [TestMethod]
    public void UseModifiedAlgorithm_WhenSetBeforeHashing_ShouldReflectNewValue()
    {
        Bernstein algorithm = new() { UseModifiedAlgorithm = true };

        Assert.IsTrue(algorithm.UseModifiedAlgorithm);
    }

}
