// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.TransformBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Test;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.TransformBlock" /> after a completed hash and reinitialization does not throw.
    /// </summary>
    [TestMethod]
    public void TransformBlock_AfterComputeHashAndInitialize_ShouldNotThrow()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        if (!algorithm.CanReuseTransform)
        {
            Assert.Inconclusive($"{typeof(TAlgorithm).Name} reports CanReuseTransform=false; reuse across hash cycles is not supported.");
            return;
        }

        _ = algorithm.ComputeHash(CryptoTestUtilities.SimpleTextAsciiBytes);
        algorithm.Initialize();

        // Should work fine as new hash cycle
        algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, 0, 16, null, 0);
        algorithm.TransformFinalBlock(CryptoTestUtilities.ByteSequence256, 16, 16);
        Assert.IsNotNull(algorithm.Hash);
    }

    /// <summary>
    /// Verifies that writable public properties on a hash algorithm throw a <see cref="CryptographicException" /> when set after a hash
    /// computation has started using <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[], int)" />.
    /// </summary>
    /// <param name="property">The property to test for immutability after hashing begins.</param>
    /// <remarks>
    /// This test uses reflection to set the property to its current hashValue after hashing has started, simulating a mutation attempt.
    /// It is intended to enforce correct behaviour in concrete <see cref="HashAlgorithm" /> or <see cref="KeyedHashAlgorithm" />
    /// implementations where configuration changes are no longer allowed once data has been processed.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(GetAlgorithmWritableProperties), DynamicDataDisplayName = nameof(TestHelpers.GetTypePropertyDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void TransformBlock_WhenPropertySetAfterTransform_ShouldThrowExactly(PropertyInfo property)
    {
        if (property is null)
        {
            Assert.Inconclusive($"Type '{typeof(TAlgorithm).Name}' has no writable properties - test passes by default.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm();

        // Begin the hashing operation
        var buffer = new byte[8];
        algorithm.TransformBlock(buffer, 0, buffer.Length, null, 0);

        object? currentValue;
        try
        {
            currentValue = property.GetValue(algorithm);
        }
        catch
        {
            Assert.Inconclusive($"Property '{property.Name}' could not be read during test setup.");
            return;
        }

        try
        {
            // Attempt to set the property to its current hashValue
            property.SetValue(algorithm, currentValue);
            Assert.Fail($"Expected CryptographicException when setting property '{property.Name}' after TransformBlock.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is CryptographicUnexpectedOperationException)
        {
            // ✅ Expected: setting config after hashing should throw
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception when setting property '{property.Name}': {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />
    /// copies the selected input bytes to the supplied output buffer.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputBufferProvided_ShouldCopyInputToOutput()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var input = CryptoTestUtilities.ByteSequence256[..32];
        var output = new byte[input.Length];

        _ = algorithm.TransformBlock(input, 0, input.Length, output, 0);

        CollectionAssert.AreEqual(input, output);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />
    /// copies the selected input bytes to the requested output offset.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldCopyInputAtOutputOffset()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var input = CryptoTestUtilities.ByteSequence256[..32];
        var output = Enumerable.Repeat((byte)0xA5, input.Length + 8).ToArray();

        var expected = Enumerable.Repeat((byte)0xA5, input.Length + 8).ToArray();
        Buffer.BlockCopy(input, 0, expected, 4, input.Length);

        _ = algorithm.TransformBlock(input, 0, input.Length, output, 4);

        CollectionAssert.AreEqual(expected, output);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />
    /// succeeds when <c>outputBuffer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputBufferIsNull_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var input = CryptoTestUtilities.ByteSequence256[..32];

        var bytesWritten = algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.AreEqual(input.Length, bytesWritten);
    }
}
