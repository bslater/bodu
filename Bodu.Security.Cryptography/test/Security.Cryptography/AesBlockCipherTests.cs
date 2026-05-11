// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the public <see cref="AesBlockCipher" /> adapter against the shared
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> suite — including BlockSize, Encrypt, Decrypt,
/// disposal, and FIPS-197 known-answer coverage across the three AES key sizes — plus a small set of
/// adapter-specific contract tests (constructor argument validation, BCL output parity, double dispose).
/// </summary>
[TestClass]
public sealed partial class AesBlockCipherTests
    : BlockCipherTests<AesBlockCipherTests, AesBlockCipher, BlockCipherKeyVariant>
{
    /// <inheritdoc />
    protected override BlockCipherSpecification GetSpecification(BlockCipherKeyVariant variant)
    {
        int keySize = variant switch
        {
            BlockCipherKeyVariant.Key128 => 16,
            BlockCipherKeyVariant.Key192 => 24,
            BlockCipherKeyVariant.Key256 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

        return new BlockCipherSpecification
        {
            BlockSize = 16,
            KeySize = keySize,
            TestKey = TestHelpers.GenerateIncrementalByteSequence(0, keySize),
        };
    }

    /// <inheritdoc />
    protected override AesBlockCipher CreateBlockCipher(BlockCipherKeyVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return new AesBlockCipher(spec.TestKey!);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(BlockCipherKeyVariant variant) =>
        AesKnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new AesBlockCipher(answer.Key!);

    /// <summary>
    /// Verifies that constructing with a <see langword="null" /> key throws
    /// <see cref="ArgumentNullException" />. AES-specific contract not covered by the generic base.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new AesBlockCipher(null!));
    }

    /// <summary>
    /// Verifies that construction fails with <see cref="CryptographicException" /> when the supplied
    /// key length is not one of the three AES variants (128 / 192 / 256 bits). The BCL surfaces the
    /// validation error as <see cref="CryptographicException" /> rather than the more general
    /// <see cref="ArgumentException" /> family used by other cipher engines, so this contract is asserted
    /// explicitly here rather than via the inherited generic tests.
    /// </summary>
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(31)]
    [DataRow(33)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keyLength)
    {
        byte[] key = new byte[keyLength];
        Assert.ThrowsExactly<CryptographicException>(() => new AesBlockCipher(key));
    }

    /// <summary>
    /// Verifies that <see cref="AesBlockCipher" /> produces byte-for-byte identical output to the
    /// BCL's <see cref="Aes.EncryptEcb(ReadOnlySpan{byte}, Span{byte}, PaddingMode)" /> — confirming
    /// the adapter applies no additional transformation beyond delegating to the underlying engine.
    /// </summary>
    [TestMethod]
    public void Encrypt_ForSingleBlock_ShouldMatchBclEncryptEcb()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        byte[] plaintext = RandomNumberGenerator.GetBytes(16);

        byte[] ourCiphertext = new byte[16];
        using (var cipher = new AesBlockCipher(key))
            cipher.Encrypt(plaintext, ourCiphertext);

        byte[] bclCiphertext = new byte[16];
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.EncryptEcb(plaintext, bclCiphertext, PaddingMode.None);
        }

        CollectionAssert.AreEqual(bclCiphertext, ourCiphertext);
    }

}
