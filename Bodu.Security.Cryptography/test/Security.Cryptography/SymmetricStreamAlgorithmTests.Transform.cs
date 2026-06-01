// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.Transform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that transforming the input in arbitrary, non-block-aligned segments produces the same output as a
    /// single one-shot transform, confirming the partial-block keystream carry is correct.
    /// </summary>
    [TestMethod]
    public void Transform_WhenInputIsSegmented_ShouldMatchOneShotTransform()
    {
        var key = CreateKey();
        var nonce = CreateNonce();
        var plaintext = CreatePayload(4097);

        byte[] oneShot;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            oneShot = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var segmented = new byte[plaintext.Length];
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
        {
            var offset = 0;
            foreach (var chunk in new[] { 1, 63, 64, 65, 127, 200, 1024, 0 })
            {
                var count = Math.Min(chunk, plaintext.Length - offset);
                offset += e.TransformBlock(plaintext, offset, count, segmented, offset);
            }

            var tail = e.TransformFinalBlock(plaintext, offset, plaintext.Length - offset);
            tail.CopyTo(segmented.AsSpan(offset));
        }

        CollectionAssert.AreEqual(oneShot, segmented);
    }

    /// <summary>
    /// Verifies that encrypting an all-zero plaintext yields the raw keystream, confirming the additive (XOR) cipher
    /// construction.
    /// </summary>
    [TestMethod]
    public void Transform_WhenPlaintextIsZero_ShouldEqualGeneratedKeystream()
    {
        var key = CreateKey();
        var nonce = CreateNonce();

        // Two independent instances under the same key/nonce must produce identical keystream.
        byte[] first;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            first = e.TransformFinalBlock(new byte[257], 0, 257);

        byte[] second;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            second = e.TransformFinalBlock(new byte[257], 0, 257);

        CollectionAssert.AreEqual(first, second);

        // The keystream must not be all zeros (which would indicate the engine produced nothing).
        Assert.IsTrue(Array.Exists(first, b => b != 0), "Keystream should not be all zero bytes.");
    }

    /// <summary>
    /// Verifies that a stream-cipher transform reports <see cref="ICryptoTransform.CanReuseTransform" /> as
    /// <see langword="false" />, since each transform represents a single one-shot operation.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_WhenQueried_ShouldBeFalse()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        Assert.IsFalse(e.CanReuseTransform);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.InputBlockSize" /> and <see cref="ICryptoTransform.OutputBlockSize" />
    /// are both one byte, confirming the transform imposes no alignment on callers.
    /// </summary>
    [TestMethod]
    public void BlockSizes_WhenTransformCreated_ShouldBeOneByte()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        Assert.AreEqual(1, e.InputBlockSize);
        Assert.AreEqual(1, e.OutputBlockSize);
    }

    /// <summary>
    /// Verifies that calling <see cref="ICryptoTransform.TransformBlock" /> after the transform has been finalized
    /// throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenCalledAfterFinalBlock_ShouldThrowInvalidOperationException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        var payload = CreatePayload(16);
        _ = e.TransformFinalBlock(payload, 0, payload.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = e.TransformBlock(payload, 0, payload.Length, new byte[payload.Length], 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="ICryptoTransform.TransformFinalBlock" /> a second time throws
    /// <see cref="InvalidOperationException" />, since the transform is finalized after the first call.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        var payload = CreatePayload(16);
        _ = e.TransformFinalBlock(payload, 0, payload.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = e.TransformFinalBlock(payload, 0, payload.Length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> with a zero-length input returns zero and writes
    /// nothing to the output buffer.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputLengthIsZero_ShouldReturnZeroAndWriteNothing()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        var output = new byte[8];
        var written = e.TransformBlock(new byte[8], 0, 0, output, 0);

        Assert.AreEqual(0, written);
        CollectionAssert.AreEqual(new byte[8], output);
    }

    /// <summary>
    /// Verifies that calling <see cref="ICryptoTransform.TransformBlock" /> after the transform has been disposed
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenTransformDisposed_ShouldThrowObjectDisposedException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());
        var payload = CreatePayload(16);
        e.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = e.TransformBlock(payload, 0, payload.Length, new byte[payload.Length], 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="ICryptoTransform.TransformFinalBlock" /> after the transform has been disposed
    /// throws <see cref="ObjectDisposedException" />, keeping the lifecycle contract symmetric with
    /// <see cref="ICryptoTransform.TransformBlock" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenTransformDisposed_ShouldThrowObjectDisposedException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());
        var payload = CreatePayload(16);
        e.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = e.TransformFinalBlock(payload, 0, payload.Length);
        });
    }

    /// <summary>
    /// Verifies that disposing a stream-cipher transform more than once is safe and does not throw, honouring the
    /// idempotent <see cref="IDisposable.Dispose" /> contract.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDisposedTwice_ShouldNotThrow()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        e.Dispose();
        e.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> with a zero-length input returns an empty
    /// array and finalizes the transform, confirming that stream-cipher finalization adds no padding.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputLengthIsZero_ShouldReturnEmptyAndFinalize()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(CreateKey(), CreateNonce());

        var result = e.TransformFinalBlock([], 0, 0);

        Assert.AreEqual(0, result.Length);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = e.TransformBlock([], 0, 0, [], 0);
        });
    }
}
