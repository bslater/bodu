// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for verifying <see cref="IPaddingStrategy" />
/// implementations. Concrete test classes supply algorithm-specific parameters via the
/// protected abstract members below.
/// </summary>
[TestClass]
public abstract partial class PaddingStrategyTests<TPadding>
    where TPadding : IPaddingStrategy, new()
{
    /// <summary>
    /// Creates a new instance of the padding strategy under test.
    /// </summary>
    protected virtual TPadding CreatePadding() => new TPadding();

    /// <summary>
    /// Gets the block size in bytes used for setting up test buffers and slicing operations.
    /// </summary>
    protected abstract int BlockSize { get; }

    /// <summary>
    /// Gets the block size in bits to use when exercising the <see cref="IPaddingStrategy.Pad"/>
    /// / <see cref="IPaddingStrategy.Unpad"/> API. Derived from <see cref="BlockSize"/>.
    /// </summary>
    protected int BlockSizeBits => BlockSize * 8;

    /// <summary>
    /// Gets a value indicating whether <c>Unpad</c> may reject corrupted padding with
    /// <see cref="CryptographicException" />. Strategies that do not validate padding
    /// (e.g. <c>NoPadding</c>) return <see langword="false" />.
    /// </summary>
    protected abstract bool ValidatesPaddingOnUnpad { get; }

    /// <summary>
    /// Gets a value indicating whether this padding strategy accepts input whose length is not a multiple of
    /// <see cref="BlockSize" /> and can recover the original plaintext through <c>Pad</c>/<c>Unpad</c>. Returns
    /// <see langword="true" /> for self-describing schemes such as PKCS#7 that can faithfully round-trip any residual length.
    /// Returns <see langword="false" /> for pass-through schemes (<c>NoPadding</c>, which rejects unaligned input) and
    /// for non-self-describing schemes (<c>ZeroPadding</c>, which cannot distinguish padding bytes from legitimate data).
    /// </summary>
    protected virtual bool SupportsUnalignedInput => true;

    /// <summary>
    /// Gets a value indicating whether <c>Unpad</c> validates the contents of the interior
    /// pad bytes (not just the trailing length byte or terminator). PKCS#7, ANSI X.923 and
    /// ISO/IEC 7816-4 all validate interior bytes; ISO 10126 does not because its interior
    /// bytes are random and cannot be reconstructed during decryption.
    /// </summary>
    protected virtual bool ValidatesInteriorPaddingOnUnpad => true;

    /// <summary>
    /// Gets a value indicating whether the padding scheme encodes its pad length in the
    /// final byte of the padded block. PKCS#7, ANSI X.923 and ISO 10126 do; ISO/IEC 7816-4
    /// uses a terminator pattern instead and returns <see langword="false" />.
    /// </summary>
    protected virtual bool HasLengthByte => true;

    /// <summary>
    /// Gets a sample plaintext that is shorter than <see cref="BlockSize" /> by the
    /// specified <paramref name="residualBytes" /> count. Used by the single-byte padding
    /// and round-trip tests.
    /// </summary>
    protected abstract byte[] CreatePlaintextWithResidual(int residualBytes);

    /// <summary>
    /// Instantiates <paramref name="algorithmType" /> via its parameterless constructor and
    /// configures it with <see cref="CipherMode.CBC" />, the requested <paramref name="padding" />,
    /// a freshly-generated key and IV, plus a tweak when the algorithm derives from
    /// <see cref="TweakableSymmetricAlgorithm" />.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type to instantiate.</param>
    /// <param name="padding">The padding mode to apply.</param>
    /// <returns>A ready-to-use <see cref="SymmetricAlgorithm" /> instance.</returns>
    protected static SymmetricAlgorithm CreateConfiguredAlgorithm(Type algorithmType, PaddingMode padding)
    {
        var algorithm = (SymmetricAlgorithm)Activator.CreateInstance(algorithmType)!;
        algorithm.Padding = padding;
        algorithm.Mode = CipherMode.CBC;
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        if (algorithm is TweakableSymmetricAlgorithm tweakable)
            tweakable.GenerateTweak();
        return algorithm;
    }

    /// <summary>
    /// Instantiates <paramref name="algorithmType"/> via its parameterless constructor and configures it with
    /// <see cref="CipherMode.CBC"/>, the requested extended <paramref name="padding"/>, a freshly-generated key and IV,
    /// plus a tweak when the algorithm derives from <see cref="TweakableSymmetricAlgorithm"/>. The <c>BlockPadding</c>
    /// property is set via reflection when the algorithm exposes it.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm"/> type to instantiate.</param>
    /// <param name="padding">The extended padding mode to apply.</param>
    /// <returns>A ready-to-use <see cref="SymmetricAlgorithm"/> instance.</returns>
    protected static SymmetricAlgorithm CreateConfiguredAlgorithm(Type algorithmType, PaddingModeKind padding)
    {
        var algorithm = (SymmetricAlgorithm)Activator.CreateInstance(algorithmType)!;
        algorithm.Mode = CipherMode.CBC;
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        if (algorithm is TweakableSymmetricAlgorithm tweakable)
            tweakable.GenerateTweak();
        System.Reflection.PropertyInfo? prop = algorithmType.GetProperty("BlockPadding");
        prop?.SetValue(algorithm, padding);
        return algorithm;
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> by writing it through a <see cref="CryptoStream" />
    /// wrapped around <paramref name="algorithm" />'s encryptor, returning the captured ciphertext.
    /// </summary>
    /// <param name="algorithm">The configured cipher algorithm.</param>
    /// <param name="plaintext">The bytes to encrypt.</param>
    /// <returns>The ciphertext bytes captured from <see cref="CryptoStream" />.</returns>
    protected static byte[] EncryptThroughCryptoStream(SymmetricAlgorithm algorithm, byte[] plaintext)
    {
        using var output = new MemoryStream();
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            if (plaintext.Length > 0)
                crypto.Write(plaintext, 0, plaintext.Length);

            crypto.FlushFinalBlock();
        }

        return output.ToArray();
    }

    /// <summary>
    /// Decrypts <paramref name="cipherText" /> by reading it through a <see cref="CryptoStream" />
    /// wrapped around <paramref name="algorithm" />'s decryptor, returning the recovered plaintext.
    /// </summary>
    /// <param name="algorithm">The configured cipher algorithm.</param>
    /// <param name="cipherText">The bytes to decrypt.</param>
    /// <returns>The recovered plaintext bytes.</returns>
    protected static byte[] DecryptThroughCryptoStream(SymmetricAlgorithm algorithm, byte[] cipherText)
    {
        using var source = new MemoryStream(cipherText);
        using ICryptoTransform decryptor = algorithm.CreateDecryptor();
        using var crypto = new CryptoStream(source, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();

        crypto.CopyTo(output);
        return output.ToArray();
    }
}
