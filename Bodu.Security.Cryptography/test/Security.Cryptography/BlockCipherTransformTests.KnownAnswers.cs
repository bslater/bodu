// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Returns the curated known-answer vectors that the Transform-layer KAT tests should drive through
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />. Default returns no vectors,
    /// in which case the encrypt and decrypt KAT tests emit a single sentinel data row and assert
    /// <see cref="Assert.Inconclusive(string)" /> rather than failing for an empty
    /// <see cref="DynamicDataAttribute" /> source.
    /// </summary>
    /// <returns>A sequence of <see cref="BlockCipherKnownAnswer" /> records, possibly empty.</returns>
    /// <remarks>
    /// Subclasses opting into Transform-layer KAT coverage typically delegate to the cipher's existing
    /// <c>&lt;Cipher&gt;KnownAnswers.For(variant)</c> accessor, optionally flattening across all defined
    /// variants. Subclasses that do not override the hook (for example, transform tests targeting
    /// wide-block constructions with no published vectors) skip the KAT path cleanly.
    /// </remarks>
    protected virtual IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Array.Empty<BlockCipherKnownAnswer>();

    /// <summary>
    /// Constructs an <see cref="ICryptoTransform" /> primed to encrypt or decrypt a single block under the
    /// supplied <paramref name="answer" /> in raw single-block ECB mode (no padding, zero IV). Override
    /// alongside <see cref="GetKnownAnswers" /> when the cipher declares Transform-layer KAT vectors.
    /// </summary>
    /// <param name="answer">The vector whose <see cref="BlockCipherKnownAnswer.Key" /> and (where
    /// applicable) <see cref="BlockCipherKnownAnswer.Tweak" /> should be applied to the algorithm before
    /// the transform is created.</param>
    /// <param name="forEncryption"><see langword="true" /> to obtain an encryptor that maps
    /// <see cref="BlockCipherKnownAnswer.Plaintext" /> to <see cref="BlockCipherKnownAnswer.Ciphertext" />;
    /// <see langword="false" /> to obtain a decryptor that maps the reverse.</param>
    /// <returns>A new <typeparamref name="TCryptoTransform" /> ready for a single-block transform.</returns>
    /// <exception cref="NotSupportedException">The default implementation throws when the subclass
    /// declares KAT vectors via <see cref="GetKnownAnswers" /> but does not override this hook.</exception>
    protected virtual TCryptoTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption) =>
        throw new NotSupportedException(
            $"Override {nameof(CreateTransformForKnownAnswer)} when {nameof(GetKnownAnswers)} returns vectors.");

    /// <summary>
    /// Yields one <see cref="DynamicDataAttribute" /> data row per vector returned by
    /// <see cref="GetKnownAnswers" /> on a freshly-constructed <typeparamref name="TTest" />, or a single
    /// sentinel row carrying <see langword="null" /> when the subclass declares no vectors.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays whose payload is either a
    /// <see cref="BlockCipherKnownAnswer" /> instance or <see langword="null" /> for the sentinel.</returns>
    public static IEnumerable<object?[]> KnownAnswerData()
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
    /// <param name="data">The single-element row produced by <see cref="KnownAnswerData" />.</param>
    /// <returns>A human-readable identifier for the row.</returns>
    public static string GetKnownAnswerDisplayName(MethodInfo methodInfo, object?[] data) =>
        data[0] is BlockCipherKnownAnswer answer
            ? string.IsNullOrEmpty(answer.Profile) ? answer.Name : $"{answer.Name} [{answer.Profile}]"
            : "(no Transform-layer KAT vectors declared)";

    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for encryption
    /// maps <see cref="BlockCipherKnownAnswer.Plaintext" /> to <see cref="BlockCipherKnownAnswer.Ciphertext" />
    /// when fed through <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />, and that the
    /// raw-block contract — single ECB block, no padding — survives the algorithm wrapper.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs (in which case the test asserts <see cref="Assert.Inconclusive(string)" />).</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenEncryptingKnownAnswer_ShouldProduceExpectedCiphertext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        byte[] actual = transform.TransformFinalBlock(answer.Plaintext, 0, answer.Plaintext.Length);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            actual,
            $"Transform KAT mismatch on encrypt for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for decryption
    /// maps <see cref="BlockCipherKnownAnswer.Ciphertext" /> back to
    /// <see cref="BlockCipherKnownAnswer.Plaintext" /> when fed through
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs (in which case the test asserts <see cref="Assert.Inconclusive(string)" />).</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenDecryptingKnownAnswer_ShouldRecoverExpectedPlaintext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: false);

        byte[] actual = transform.TransformFinalBlock(answer.Ciphertext, 0, answer.Ciphertext.Length);

        CollectionAssert.AreEqual(
            answer.Plaintext,
            actual,
            $"Transform KAT mismatch on decrypt for vector '{answer.Name}'.");
    }
}
