// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
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
    /// <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> against known input-output pairs.
    /// </remarks>
    public static IEnumerable<object[]> EncryptTestData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            IEnumerable<KnownAnswerTest> testVectors = instance.GetKnownAnswerTests(variant);
            foreach (KnownAnswerTest vector in testVectors)
            {
                yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput, vector.CipherFactory! };
            }
        }
    }

    public static string GetEncryptTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 4)
            throw new ArgumentException("Expected variant and test name.", nameof(data));

        var variant = (TVariant)data[0];
        var blockSize = (byte[])data[3];
        return $"Block Size:{blockSize.Length} (Variant: {variant})";
    }

    public static IEnumerable<object[]> GetInvalidBlockSizes()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            var blockSize = instance.GetSpecification(variant).BlockSize;

            yield return new object[] { variant, new byte[0] };
            yield return new object[] { variant, new byte[blockSize - 1] };
            yield return new object[] { variant, new byte[blockSize + 1] };
        }
    }

    public static string GetValidSingleBlockTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 3)
            throw new ArgumentException("Expected variant and test name.", nameof(data));

        var variant = (TVariant)data[0];
        var blockSize = (byte[])data[2];
        return $"Block Size:{blockSize.Length} (Variant: {variant})";
    }

    public static IEnumerable<object[]> GetValidSingleBlockData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetBlockCipherVariants())
        {
            var blockSize = instance.GetSpecification(variant).BlockSize;

            yield return new object[] { variant, "All Zeros", new byte[blockSize] };
            yield return new object[] { variant, "Ascending Bytes", Enumerable.Range(0, blockSize).Select(i => (byte)i).ToArray() };
            yield return new object[] { variant, "All 0xFF", Enumerable.Repeat((byte)0xFF, blockSize).ToArray() };
            yield return new object[] { variant, "Alternating 0xAA / 0x55", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xAA : 0x55)).ToArray() };
            yield return new object[] { variant, "Alternating 0xFF / 0x00", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xFF : 0x00)).ToArray() };
            yield return new object[] { variant, "Alternating 0xF0 / 0x0F", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 2 == 0 ? 0xF0 : 0x0F)).ToArray() };
            yield return new object[] { variant, "Sawtooth 0x00–0x0F", Enumerable.Range(0, blockSize).Select(i => (byte)(i % 16)).ToArray() };
            yield return new object[] { variant, "Mirrored Half Asc/Desc", Enumerable.Range(0, blockSize).Select(i => (byte)(i < blockSize / 2 ? i : blockSize - i - 1)).ToArray() };
        }
    }

    public static string GetInvalidBlockSizeTestDisplayName(MethodInfo methodInfo, object[] data)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 2)
            throw new ArgumentException("Expected variant and test name.", nameof(data));

        var variant = (TVariant)data[0];
        var blockSize = (byte[])data[1];
        return $"Block Size:{blockSize.Length} (Variant: {variant})";
    }

    /// <summary>
    /// Verifies that encryption does not alter the input span.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalled_ShouldNotModifyInputBuffer()
    {
        using TCipher cipher = CreateBlockCipher();
        var original = TestHelpers.GenerateRandomNonZeroBytes(cipher.BlockSize / 8);
        var input = original.ToArray();
        var output = new byte[cipher.BlockSize / 8];

        cipher.Encrypt(input, output);

        CollectionAssert.AreEqual(original, input); // input must be unchanged
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> across diferent instances
    /// with the same input produce the same result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Encrypt_WhenCalled_WithDiferentInstances_ShouldBeDeterministic(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher1 = CreateBlockCipher(variant);
        using TCipher cipher2 = CreateBlockCipher(variant);

        var input = TestHelpers.GenerateRandomNonZeroBytes(cipher1.BlockSize / 8);

        var output1 = new byte[specification.BlockSize];
        var output2 = new byte[specification.BlockSize];

        cipher1.Encrypt(input, output1);
        cipher2.Encrypt(input, output2);

        CollectionAssert.AreEqual(output1, output2);
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="IBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> using the same instances and
    /// input produce the same result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Encrypt_WhenCalled_WithSameInstsnce_ShouldBeDeterministic(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        var input = TestHelpers.GenerateRandomNonZeroBytes(cipher.BlockSize / 8);

        var output1 = new byte[specification.BlockSize];
        var output2 = new byte[specification.BlockSize];

        cipher.Encrypt(input, output1);
        cipher.Encrypt(input, output2);

        CollectionAssert.AreEqual(output1, output2);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipher.Encrypt" />, when KnownInput, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(EncryptTestData), DynamicDataDisplayName = nameof(GetEncryptTestDisplayName))]
    public void Encrypt_WhenKnownInput_ShouldMatchExpected(TVariant variant, string testName, byte[] input, byte[] expected, Func<IBlockCipher>? factory)
    {
        ArgumentNullException.ThrowIfNull(expected);
        IBlockCipher engine = factory?.Invoke() ?? CreateBlockCipher(variant);
        var actual = new byte[expected.Length];
        engine.Encrypt(input, actual);

        TestHelpers.TraceWriteIfNotEqual(expected, actual);

        CollectionAssert.AreEqual(expected, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
    }

    /// <summary>
    /// Verifies that encryption throws ArgumentException when input size is invalid.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataDisplayName = nameof(GetInvalidBlockSizeTestDisplayName))]
    public void Encrypt_WithInvalidInputSize_ShouldThrowExactly(TVariant variant, byte[] input)
    {
        using TCipher cipher = CreateBlockCipher(variant);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Encrypt(input, new byte[cipher.BlockSize / 8]);
        });
    }

    /// <summary>
    /// Verifies that encryption throws ArgumentException when output size is invalid.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetInvalidBlockSizes), DynamicDataDisplayName = nameof(GetInvalidBlockSizeTestDisplayName))]
    public void Encrypt_WithInvalidOutSize_ShouldThrowExactly(TVariant variant, byte[] output)
    {
        using TCipher cipher = CreateBlockCipher(variant);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Encrypt(new byte[cipher.BlockSize / 8], output);
        });
    }

    /// <summary>
    /// Verifies that encryption and decryption can operate on the same buffer (in-place).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void EncryptDecrypt_WithInPlaceBuffer_ShouldSucceed(TVariant variant)
    {
        using TCipher cipher = CreateBlockCipher();

        var original = TestHelpers.GenerateRandomNonZeroBytes(cipher.BlockSize / 8);
        var buffer = (byte[])original.Clone();

        cipher.Encrypt(buffer, buffer);
        cipher.Decrypt(buffer, buffer);

        CollectionAssert.AreEqual(original, buffer);
    }

    /// <summary>
    /// Verifies that encryption and decryption of valid blocks succeeds without exceptions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidSingleBlockData), DynamicDataDisplayName = nameof(GetValidSingleBlockTestDisplayName))]
    public void EncryptDecrypt_WithValidInput_ShouldRoundtrip(TVariant variant, string testName, byte[] input)
    {
        using TCipher cipher = CreateBlockCipher(variant);

        var encrypted = new byte[cipher.BlockSize / 8];
        cipher.Encrypt(input, encrypted);
        var actual = new byte[cipher.BlockSize / 8];
        cipher.Decrypt(encrypted, actual);

        CollectionAssert.AreEqual(input, actual, $"Cipher mismatch for {testName} using variant '{variant}'.");
    }
}
