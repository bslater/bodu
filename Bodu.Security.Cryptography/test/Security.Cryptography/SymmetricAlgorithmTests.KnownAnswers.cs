// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Returns the curated known-answer vectors that the Algorithm-layer KAT tests should drive through
    /// <see cref="SymmetricAlgorithm.CreateEncryptor()" /> and <see cref="SymmetricAlgorithm.CreateDecryptor()" />.
    /// Default returns no vectors, in which case the encrypt and decrypt KAT tests emit a single sentinel
    /// data row and assert <see cref="Assert.Inconclusive(string)" /> rather than failing for an empty
    /// <see cref="DynamicDataAttribute" /> source.
    /// </summary>
    /// <returns>A sequence of <see cref="BlockCipherKnownAnswer" /> records, possibly empty.</returns>
    /// <remarks>
    /// Subclasses opting into Algorithm-layer KAT coverage typically delegate to the cipher's existing
    /// <c>&lt;Cipher&gt;KnownAnswers.For(variant)</c> accessor, optionally flattening across all defined
    /// variants. Subclasses without published vectors (for example, the wide-block Serpent algorithm
    /// tests) inherit the empty default and the harness records each KAT row as inconclusive.
    /// </remarks>
    protected virtual IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Array.Empty<BlockCipherKnownAnswer>();

    /// <summary>
    /// Constructs a fully configured <typeparamref name="TAlgorithm" /> instance ready to encrypt or
    /// decrypt the supplied <paramref name="answer" /> in raw single-block ECB mode (no padding, zero IV).
    /// Override alongside <see cref="GetKnownAnswers" /> when the cipher declares Algorithm-layer KAT
    /// vectors. Must apply the answer's <see cref="BlockCipherKnownAnswer.Key" /> and (where applicable)
    /// <see cref="BlockCipherKnownAnswer.Tweak" /> before returning, so a subsequent call to
    /// <see cref="SymmetricAlgorithm.CreateEncryptor()" /> / <see cref="SymmetricAlgorithm.CreateDecryptor()" />
    /// uses the correct material.
    /// </summary>
    /// <param name="answer">The vector whose key (and tweak, if applicable) should be applied.</param>
    /// <returns>A new <typeparamref name="TAlgorithm" /> primed for a single-block transform.</returns>
    /// <exception cref="NotSupportedException">The default implementation throws when the subclass declares
    /// KAT vectors via <see cref="GetKnownAnswers" /> but does not override this hook.</exception>
    protected virtual TAlgorithm CreateAlgorithmForKnownAnswer(BlockCipherKnownAnswer answer) =>
        throw new NotSupportedException(
            $"Override {nameof(CreateAlgorithmForKnownAnswer)} when {nameof(GetKnownAnswers)} returns vectors.");

    /// <summary>
    /// Yields one <see cref="DynamicDataAttribute" /> data row per vector returned by
    /// <see cref="GetKnownAnswers" /> on a freshly-constructed <typeparamref name="TTest" />, or a single
    /// sentinel row carrying <see langword="null" /> when the subclass declares no vectors.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays whose payload is either a
    /// <see cref="BlockCipherKnownAnswer" /> instance or <see langword="null" /> for the sentinel.</returns>
    public static IEnumerable<object?[]> AlgorithmKnownAnswerData()
    {
        TTest instance = new();
        BlockCipherKnownAnswer[] answers = instance.GetKnownAnswers().ToArray();
        if (answers.Length == 0)
        {
            yield return new object?[] { null };
            yield break;
        }

        foreach (BlockCipherKnownAnswer answer in answers)
            yield return new object?[] { answer };
    }

    /// <summary>
    /// Resolves a friendly display name for a <see cref="DynamicDataAttribute" /> data row, surfacing the
    /// vector's <see cref="BlockCipherKnownAnswer.Name" /> (and <see cref="BlockCipherKnownAnswer.Profile" />
    /// when present) so test reports identify the failing row at a glance.
    /// </summary>
    /// <param name="methodInfo">The test method being parameterised — required by the MSTest signature.</param>
    /// <param name="data">The single-element row produced by <see cref="AlgorithmKnownAnswerData" />.</param>
    /// <returns>A human-readable identifier for the row.</returns>
    public static string GetAlgorithmKnownAnswerDisplayName(MethodInfo methodInfo, object?[] data) =>
        data[0] is BlockCipherKnownAnswer answer
            ? string.IsNullOrEmpty(answer.Profile) ? answer.Name : $"{answer.Name} [{answer.Profile}]"
            : "(no Algorithm-layer KAT vectors declared)";

    /// <summary>
    /// Verifies that an encryptor obtained from a fully configured <typeparamref name="TAlgorithm" /> via
    /// <see cref="SymmetricAlgorithm.CreateEncryptor()" /> maps <see cref="BlockCipherKnownAnswer.Plaintext" />
    /// to <see cref="BlockCipherKnownAnswer.Ciphertext" /> when fed through
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> in raw single-block ECB mode.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Algorithm-layer KATs (in which case the test asserts <see cref="Assert.Inconclusive(string)" />).</param>
    [TestMethod]
    [DynamicData(nameof(AlgorithmKnownAnswerData), DynamicDataDisplayName = nameof(GetAlgorithmKnownAnswerDisplayName))]
    public void CreateEncryptor_WhenUsingKnownAnswer_ShouldProduceExpectedCiphertext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Algorithm-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithmForKnownAnswer(answer);
        using ICryptoTransform encryptor = algorithm.CreateEncryptor();

        byte[] actual = encryptor.TransformFinalBlock(answer.Plaintext, 0, answer.Plaintext.Length);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            actual,
            $"Algorithm KAT mismatch on encrypt for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that a decryptor obtained from a fully configured <typeparamref name="TAlgorithm" /> via
    /// <see cref="SymmetricAlgorithm.CreateDecryptor()" /> maps <see cref="BlockCipherKnownAnswer.Ciphertext" />
    /// back to <see cref="BlockCipherKnownAnswer.Plaintext" /> when fed through
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> in raw single-block ECB mode.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Algorithm-layer KATs (in which case the test asserts <see cref="Assert.Inconclusive(string)" />).</param>
    [TestMethod]
    [DynamicData(nameof(AlgorithmKnownAnswerData), DynamicDataDisplayName = nameof(GetAlgorithmKnownAnswerDisplayName))]
    public void CreateDecryptor_WhenUsingKnownAnswer_ShouldRecoverExpectedPlaintext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Algorithm-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithmForKnownAnswer(answer);
        using ICryptoTransform decryptor = algorithm.CreateDecryptor();

        byte[] actual = decryptor.TransformFinalBlock(answer.Ciphertext, 0, answer.Ciphertext.Length);

        CollectionAssert.AreEqual(
            answer.Plaintext,
            actual,
            $"Algorithm KAT mismatch on decrypt for vector '{answer.Name}'.");
    }
}
