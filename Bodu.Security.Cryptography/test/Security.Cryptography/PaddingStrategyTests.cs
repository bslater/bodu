// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
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

    /// <summary>
    /// Provides one row per concrete <see cref="SymmetricAlgorithm" /> declared in the
    /// <c>Bodu.Security.Cryptography</c> assembly that exposes a public parameterless
    /// constructor. Use via <c>[DynamicData(nameof(SymmetricAlgorithmTestData), DynamicDataDisplayName = nameof(GetSymmetricAlgorithmDisplayName))]</c>
    /// on any padding-strategy test that wants to assert a contract for every cipher in
    /// the library — empty-input round-trips through <see cref="System.Security.Cryptography.CryptoStream" />,
    /// for example.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is the <see cref="Type" />
    /// of a <see cref="SymmetricAlgorithm" />-derived sealed class. Tests instantiate the type
    /// via <see cref="Activator.CreateInstance(Type)" />.
    /// </returns>
    public static IEnumerable<object[]> SymmetricAlgorithmTestData()
    {
        Assembly assembly = typeof(IPaddingStrategy).Assembly;
        foreach (Type type in assembly.GetTypes()
            .Where(IsConstructibleSymmetricAlgorithm)
            .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            yield return new object[] { type };
        }
    }

    /// <summary>
    /// Renders the <see cref="SymmetricAlgorithm" /> type name for a row produced by
    /// <see cref="SymmetricAlgorithmTestData" />, so MSTest's test explorer surfaces the
    /// per-cipher row name (e.g. <c>Skipjack</c>) instead of the default ordinal.
    /// </summary>
    /// <param name="methodInfo">The test method (unused; required by <see cref="DynamicDataAttribute" />).</param>
    /// <param name="data">The single-element row produced by <see cref="SymmetricAlgorithmTestData" />.</param>
    /// <returns>The simple type name of the row's <see cref="Type" /> argument.</returns>
    public static string GetSymmetricAlgorithmDisplayName(MethodInfo methodInfo, object[] data) =>
        ((Type)data[0]).Name;

    /// <summary>
    /// Filters a candidate <see cref="Type" /> down to the concrete, parameterless,
    /// <see cref="SymmetricAlgorithm" />-derived classes shipped by
    /// <c>Bodu.Security.Cryptography</c>.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="type" /> is a non-abstract,
    /// non-generic class that derives from <see cref="SymmetricAlgorithm" /> and exposes a
    /// public parameterless constructor; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsConstructibleSymmetricAlgorithm(Type type) =>
        type.IsClass
        && !type.IsAbstract
        && !type.IsGenericTypeDefinition
        && typeof(SymmetricAlgorithm).IsAssignableFrom(type)
        && type.GetConstructor(Type.EmptyTypes) is not null;
}
