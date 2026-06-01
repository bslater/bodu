// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Decrypt.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Extensions;
using Bodu.Test;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Returns test data that combines algorithm variants, named inputs, and expected hash results.
    /// </summary>
    /// <returns>A sequence of test case arguments: variant, input name, input bytes, and expected hash output.</returns>
    /// <remarks>
    /// This method is used to parameterize tests that verify the correctness of
    /// <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> against known input-output pairs.
    /// </remarks>
    public static IEnumerable<object[]> DecryptTestData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            IEnumerable<KnownAnswerTest> testVectors = instance.GetKnownAnswerTests(variant);
            foreach (KnownAnswerTest vector in testVectors)
            {
                yield return new object[] { variant, vector.Name, vector.ExpectedOutput, vector.Input, vector.CipherFactory! };
            }
        }
    }

    public static string GetDecryptTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 2)
            throw new ArgumentException("Expected variant and test name.", nameof(data));

        return $"{(string)data[1]} (Variant: {(TVariant)data[0]})";
    }

    /// <summary>
    /// Verifies that decryption does not alter the input span.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalled_ShouldNotModifyInputBuffer()
    {
        using TCipher cipher = CreateBlockCipher();
        var original = TestHelpers.GenerateRandomNonZeroBytes(cipher.BlockSize / 8);
        var input = original.ToArray();
        var output = new byte[cipher.BlockSize / 8];

        cipher.Decrypt(input, output);

        CollectionAssert.AreEqual(original, input); // input must be unchanged
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> across diferent instances
    /// with the same input produce the same result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Decrypt_WhenCalled_WithDiferentInstances_ShouldBeDeterministic(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher1 = CreateBlockCipher(variant);
        using TCipher cipher2 = CreateBlockCipher(variant);

        var input = TestHelpers.GenerateRandomNonZeroBytes(cipher1.BlockSize / 8);

        var output1 = new byte[specification.BlockSize];
        var output2 = new byte[specification.BlockSize];

        cipher1.Decrypt(input, output1);
        cipher2.Decrypt(input, output2);

        CollectionAssert.AreEqual(output1, output2);
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="IBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> using the same instances and
    /// input produce the same result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Decrypt_WhenCalled_WithSameInstsnce_ShouldBeDeterministic(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        var input = TestHelpers.GenerateRandomNonZeroBytes(cipher.BlockSize / 8);

        var output1 = new byte[specification.BlockSize];
        var output2 = new byte[specification.BlockSize];

        cipher.Decrypt(input, output1);
        cipher.Decrypt(input, output2);

        CollectionAssert.AreEqual(output1, output2);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipher.Decrypt" />, when KnownInput, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DecryptTestData), DynamicDataDisplayName = nameof(GetDecryptTestDisplayName))]
    public void Decrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected, Func<IBlockCipher>? factory)
    {
        ArgumentNullException.ThrowIfNull(expected);
        IBlockCipher engine = factory?.Invoke() ?? CreateBlockCipher(variant);
        var actual = new byte[expected.Length];
        engine.Decrypt(input, actual);

        TestHelpers.TraceWriteIfNotEqual(expected, actual);

        CollectionAssert.AreEqual(expected, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
    }

    /// <summary>
    /// Verifies that cedryption throws ArgumentException when input size is invalid.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataDisplayName = nameof(GetInvalidBlockSizeTestDisplayName))]
    public void Decrypt_WithInvalidInputSize_ShouldThrowExactly(TVariant variant, byte[] input)
    {
        using TCipher cipher = CreateBlockCipher(variant);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Decrypt(input, new byte[cipher.BlockSize / 8]);
        });
    }

    /// <summary>
    /// Verifies that cedryption throws ArgumentException when output size is invalid.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInvalidBlockSizes))]
    public void Decrypt_WithInvalidOutSize_ShouldThrowExactly(TVariant variant, byte[] output)
    {
        using TCipher cipher = CreateBlockCipher(variant);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Decrypt(new byte[cipher.BlockSize / 8], output);
        });
    }
}
