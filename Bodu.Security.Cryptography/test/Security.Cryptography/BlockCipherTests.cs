// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base test class for <see cref="IBlockCipher" /> engines. Owns the data-driven encrypt / decrypt
/// known-answer harness, the boundary-length and round-trip tests, and the disposal-state assertions
/// shared by every cipher family in the suite.
/// </summary>
/// <typeparam name="TTest">The concrete test class — used to <c>new TTest()</c> from
/// <see cref="DynamicDataAttribute" /> sources so static data-row generators can dispatch to instance
/// overrides.</typeparam>
/// <typeparam name="TCipher">The concrete <see cref="IBlockCipher" /> engine under test.</typeparam>
/// <typeparam name="TVariant">The cipher's configuration enum — typically <c>SingleTestVariant</c>,
/// <see cref="BlockCipherKeyVariant" />, or <see cref="TweakableBlockCipherVariant" />.</typeparam>
/// <remarks>
/// <para>
/// This is the entry point of the three-layer cipher test architecture. KAT vectors curated in a
/// per-cipher <c>&lt;Cipher&gt;KnownAnswers</c> static class (for example <c>SkipjackKnownAnswers</c>,
/// <c>CamelliaKnownAnswers</c>) are consumed at the block-cipher layer here, and the same vectors flow
/// through <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> and
/// <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" /> at the upper test layers.
/// </para>
/// <para>
/// Concrete subclasses override four canonical hooks: <see cref="GetSpecification" /> describing the variant
/// under test, <see cref="CreateBlockCipher(TVariant)" /> constructing the engine for boundary-length tests,
/// <see cref="GetKnownAnswers" /> returning the curated KAT vector list, and
/// <see cref="CreateBlockCipherForAnswer" /> constructing a per-vector engine bound to that row's key and
/// tweak. The base class supplies <see cref="GetBlockCipherVariants" /> from <see cref="Enum.GetValues(Type)" />
/// — override only to deliberately exclude an enumeration member — and the default
/// <see cref="GetKnownAnswerTests" /> wires the two KAT hooks together via <see cref="AdaptKnownAnswers" />.
/// Override <see cref="GetKnownAnswerTests" /> directly only when vectors are runtime-generated rather than
/// sourced from a static <c>&lt;Cipher&gt;KnownAnswers</c> class — see <see cref="BlockCipherKnownAnswer" />
/// for the full architecture overview.
/// </para>
/// </remarks>
[TestClass]
public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
    where TTest : BlockCipherTests<TTest, TCipher, TVariant>, new()
    where TCipher : IBlockCipher
    where TVariant : Enum
{
    /// <summary>
    /// Gets the default variant to use in non-parameterized test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the block cipher under test. It is used for tests
    /// that do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetBlockCipherVariants().First();

    /// <summary>
    /// Returns the <see cref="BlockCipherSpecification" /> describing the expected properties of
    /// <typeparamref name="TBlockCipher" /> when constructed for the given <paramref name="variant" />.
    /// </summary>
    protected abstract BlockCipherSpecification GetSpecification(TVariant variant);

    /// <summary>
    /// Returns test case parameters for each defined block cipher variant.
    /// </summary>
    /// <returns>An enumerable of <see cref="TVariant" /> values wrapped in object arrays.</returns>
    public static IEnumerable<object[]> BlockCipherVariants() =>
        new TTest().GetBlockCipherVariants().Select(variant => new object[] { variant });

    /// <summary>
    /// Returns all supported block cipher variants to be tested for the current implementation.
    /// </summary>
    /// <returns>
    /// A sequence of <typeparamref name="TVariant" /> values representing valid configuration variants for the block cipher under test.
    /// </returns>
    /// <remarks>
    /// This method drives variant-specific tests. Each variant may represent a change in output size, internal round configuration, or
    /// other block cipher-specific mode flags. The default implementation returns every member of <typeparamref name="TVariant" /> via
    /// <see cref="Enum.GetValues(Type)" />, which is the right behaviour for every cipher family in the suite — override only when a
    /// cipher needs to deliberately exclude an enumeration member from its test matrix.
    /// </remarks>
    public virtual IEnumerable<TVariant> GetBlockCipherVariants() =>
        Enum.GetValues(typeof(TVariant)).Cast<TVariant>();


    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedFieldNames => [];

    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedFieldNames() =>
        ExcludedFieldNames
            .Concat([
                // excluded field names that are exposed in Bodu and .Net types
                "disposed",
                "_disposed"
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Enumerates all instance fields in the algorithm and its base types to validate disposal state.
    /// </summary>
    public static IEnumerable<object[]> GetAlgorithmFields() =>
        TestHelpers.GetFieldInfoForType<TCipher>(
            excludeFields: new TTest().GetExcludedFieldNames()?.ToArray() ?? []);


    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedReadablePropertyNames => [];

    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedWriteablePropertyNames => [];

    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedReadablePropertyNames() =>
        ExcludedReadablePropertyNames
            .Concat([
                // excluded property names
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedWriteablePropertyNames() =>
        ExcludedWriteablePropertyNames
            .Concat([
                // excluded property names
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Returns all publicly readable properties on <typeparamref name="TCipher" /> as test data for disposal validation.
    /// </summary>
    public static IEnumerable<object[]> GetAlgorithmReadableProperties() =>
        TestHelpers.GetPropertyInfoForType<TCipher>(
            TestHelpers.PropertyAccessMode.Read,
            excludeProperties: new TTest().GetExcludedReadablePropertyNames()?.ToArray() ?? []);

    /// <summary>
    /// Returns all publicly writable properties on <typeparamref name="TCipher" /> as test data for disposal validation.
    /// </summary>
    public static IEnumerable<object[]> GetAlgorithmWritableProperties() =>
        TestHelpers.GetPropertyInfoForType<TCipher>(
            TestHelpers.PropertyAccessMode.Write,
            excludeProperties: new TTest().GetExcludedWriteablePropertyNames()?.ToArray() ?? []);

    /// <summary>
    /// Creates a new instance of the block cipher engine under test using the default variant.
    /// </summary>
    /// <returns>A fully initialised instance of <typeparamref name="TCipher" /> configured with <see cref="DefaultVariant" />.</returns>
    protected virtual TCipher CreateBlockCipher() =>
        CreateBlockCipher(DefaultVariant);

    /// <summary>
    /// Creates a new instance of the block cipher for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant to instantiate.</param>
    /// <returns>A new instance of <typeparamref name="TCipher" /> configured for the given variant.</returns>
    protected abstract TCipher CreateBlockCipher(TVariant variant);

    /// <summary>
    /// Returns the <see cref="BlockCipherKnownAnswer" /> vectors for <paramref name="variant" /> sourced from the
    /// per-cipher <c>&lt;Cipher&gt;KnownAnswers</c> static class. The default implementation returns an empty list, which
    /// is appropriate for ciphers that have no published vectors and rely on inherited round-trip and determinism
    /// coverage instead.
    /// </summary>
    /// <param name="variant">The variant whose vectors should be returned.</param>
    /// <returns>The KAT vectors for <paramref name="variant" />, or an empty list if none are published.</returns>
    /// <remarks>
    /// This is the canonical hook for cipher families with static published vectors — Skipjack, Blowfish, Camellia,
    /// Twofish, Threefish 256/512/1024, Serpent-128, AES, and so on. Override <see cref="GetKnownAnswerTests" />
    /// directly only when the vectors are runtime-generated (for example the wide-block Serpent self-referential
    /// regression rows).
    /// </remarks>
    protected virtual IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(TVariant variant) =>
        Array.Empty<BlockCipherKnownAnswer>();

    /// <summary>
    /// Constructs an <see cref="IBlockCipher" /> instance configured for a single KAT vector — applying its
    /// <see cref="BlockCipherKnownAnswer.Key" /> and, for tweakable ciphers, its
    /// <see cref="BlockCipherKnownAnswer.Tweak" />. The default implementation throws and must be overridden when
    /// <see cref="GetKnownAnswers" /> returns a non-empty list.
    /// </summary>
    /// <param name="answer">The vector for which to construct an engine.</param>
    /// <returns>A new <see cref="IBlockCipher" /> instance ready to encrypt or decrypt <paramref name="answer" />.</returns>
    /// <exception cref="NotSupportedException">The concrete test class returns KAT vectors via
    /// <see cref="GetKnownAnswers" /> but does not override this hook.</exception>
    protected virtual IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        throw new NotSupportedException(
            $"{GetType().Name} returns KAT vectors via {nameof(GetKnownAnswers)} but does not override {nameof(CreateBlockCipherForAnswer)}.");

    /// <summary>
    /// Generates the runnable <see cref="KnownAnswerTest" /> rows for <paramref name="variant" />. The default
    /// implementation bridges <see cref="GetKnownAnswers" /> through <see cref="AdaptKnownAnswers" /> and
    /// <see cref="CreateBlockCipherForAnswer" /> — the canonical shape for every cipher with static vectors.
    /// Override directly when the vectors are runtime-generated.
    /// </summary>
    /// <param name="variant">The variant to generate test vectors for.</param>
    /// <returns>
    /// A sequence of <see cref="KnownAnswerTest" /> instances representing named test inputs and their expected
    /// ciphertext results.
    /// </returns>
    protected virtual IEnumerable<KnownAnswerTest> GetKnownAnswerTests(TVariant variant) =>
        AdaptKnownAnswers(GetKnownAnswers(variant), CreateBlockCipherForAnswer);

    /// <summary>
    /// Adapts a sequence of <see cref="BlockCipherKnownAnswer" /> vectors into the legacy
    /// <see cref="KnownAnswerTest" /> shape consumed by the existing encrypt/decrypt KAT harness.
    /// </summary>
    /// <param name="answers">The data-driven vectors sourced from a per-cipher <c>&lt;Cipher&gt;KnownAnswers</c> static class.</param>
    /// <param name="cipherFactory">A factory that constructs an <see cref="IBlockCipher" /> instance for a given vector,
    /// applying its <see cref="BlockCipherKnownAnswer.Key" /> and (where applicable) <see cref="BlockCipherKnownAnswer.Tweak" />.</param>
    /// <returns>A lazily-evaluated sequence of <see cref="KnownAnswerTest" /> rows wrapping each supplied vector.</returns>
    /// <remarks>
    /// Acts as the bridge between the externalised KAT data files (modelled on the Skein hash refactor) and the
    /// existing <c>EncryptTestData</c> / <c>DecryptTestData</c> pipelines. Each emitted row carries a per-vector
    /// <see cref="KnownAnswerTest.CipherFactory" /> that defers cipher construction until the test executes.
    /// </remarks>
    protected static IEnumerable<KnownAnswerTest> AdaptKnownAnswers(
        IEnumerable<BlockCipherKnownAnswer> answers,
        Func<BlockCipherKnownAnswer, IBlockCipher> cipherFactory) =>
        answers.Select(answer => new KnownAnswerTest
        {
            Name = answer.Name,
            Input = answer.Plaintext,
            ExpectedOutput = answer.Ciphertext,
            CipherFactory = () => cipherFactory(answer),
        });
}
