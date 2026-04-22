// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// Gets the block size in bytes to use when exercising the Pad/Unpad API.
    /// </summary>
    protected abstract int BlockSize { get; }

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
}
