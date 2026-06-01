// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Variant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that setting <see cref="Pearson.Variant" /> to a valid predefined type allows computing a algorithm without error, and
    /// that the algorithm output is the correct length for the current <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    /// <param name="variant">The table type to test.</param>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(HashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(HashAlgorithmVariantDisplayName))]
    public void Variant_Set_WhenValid_ShouldProduceExpectedHash(TigerVariant variant)
    {
        using Tiger algorithm = CreateAlgorithm(variant);

        var input = Encoding.ASCII.GetBytes("Test input");

        var result = algorithm.ComputeHash(input);

        Assert.IsNotNull(result, $"Result should not be null for table variant {variant}.");
        Assert.AreEqual(algorithm.HashSize / 8, result.Length, $"Hash length should match expected size for variant {variant}.");
        Assert.IsTrue(result.Any(b => b != 0), $"Hash result should not be all zeros for variant {variant}.");
    }

    /// <summary>
    /// Verifies that a freshly constructed <see cref="Tiger" /> reports its default <see cref="Tiger.Variant" /> as <see cref="TigerHashingVariant.Tiger" />.
    /// </summary>
    [TestMethod]
    public void Variant_Get_WhenDefault_ShouldReturnPearson()
    {
        using Tiger algorithm = CreateAlgorithm();

        TigerHashingVariant type = algorithm.Variant;

        Assert.AreEqual(Bodu.Security.Cryptography.TigerHashingVariant.Tiger, type, "Default Variant should be Tiger.");
    }

    /// <summary>
    /// Verifies that <see cref="Tiger.Variant" />, Set, when HashingStarted, throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Variant_Set_WhenHashingStarted_ShouldThrowExactly()
    {
        using Tiger algorithm = CreateAlgorithm();
        algorithm.TransformBlock([1, 2, 3], 0, 3, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.Variant = Bodu.Security.Cryptography.TigerHashingVariant.Tiger2;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="Tiger.Variant" /> on a disposed algorithm throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Variant_Set_WhenDisposed_ShouldThrowExactly()
    {
        using Tiger algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Variant = Bodu.Security.Cryptography.TigerHashingVariant.Tiger2;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="Tiger.Variant" /> on a disposed algorithm throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Variant_Get_WhenDisposed_ShouldThrowExactly()
    {
        using Tiger algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            TigerHashingVariant _ = algorithm.Variant;
        });
    }

    /// <summary>
    /// Verifies that assigning an undefined <see cref="TigerHashingVariant" /> value to
    /// <see cref="Tiger.Variant" /> throws <see cref="ArgumentOutOfRangeException" /> via the
    /// enum-defined-value guard rather than a raw cast pass-through.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void Variant_WhenSetToUndefinedEnum_ShouldThrowExactly(int rawValue)
    {
        using Tiger algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Variant = (TigerHashingVariant)rawValue;
        });
    }
}
