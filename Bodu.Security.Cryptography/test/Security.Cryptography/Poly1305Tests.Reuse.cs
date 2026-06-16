// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.Reuse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contract tests that pin down <see cref="Poly1305" />'s one-time-key semantics: the same instance must
/// not silently produce a second tag, all derived secret state must be cleared on disposal, and the
/// instance must remain usable if the caller explicitly assigns a fresh key for the next message.
/// </summary>
public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[])" /> a second time on the same
    /// <see cref="Poly1305" /> instance — without reassigning <see cref="Poly1305.Key" /> — throws rather
    /// than silently producing a tag derived from the zeroed (post-finalize) key material.
    /// </summary>
    /// <remarks>
    /// Poly1305 is a one-time MAC. Reusing a polynomial key against a second message lets an attacker recover
    /// the polynomial key <c>r</c> from a single tag pair, after which they can forge tags at will. The class
    /// already clears <see cref="Poly1305"/>'s key buffer at the end of <c>ProcessFinalBlock</c>, but on
    /// .NET 6+ the base-class <c>_finalized</c> guard is compiled out — so this test pins the contract.
    /// </remarks>
    [TestMethod]
    public void ComputeHash_WhenCalledTwiceWithoutNewKey_ShouldThrow()
    {
        using var poly = new Poly1305 { Key = (byte[])Poly1305TestKey.Clone() };
        byte[] message = System.Text.Encoding.ASCII.GetBytes("first message");

        _ = poly.ComputeHash(message);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = poly.ComputeHash(message);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Dispose()" /> overwrites the derived polynomial key schedule
    /// (<c>_r</c>, <c>_s</c>, <c>_key</c>) and the running accumulator <c>_acc</c> so that no residual key
    /// material remains observable through reflection after the instance is disposed.
    /// </summary>
    /// <remarks>
    /// The schedule is captured immediately after the <c>Key</c> setter populates it — not after a hash
    /// computation — because the post-finalize auto-Initialize re-derives the schedule from the now-zero
    /// <c>KeyValue</c> and would mask the dispose-clearing contract being asserted here.
    /// </remarks>
    [TestMethod]
    public void Dispose_ShouldClearDerivedKeyScheduleAndAccumulator()
    {
        var poly = new Poly1305 { Key = (byte[])Poly1305TestKey.Clone() };

        FieldInfo? rField = typeof(Poly1305).GetField("_r", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? sField = typeof(Poly1305).GetField("_s", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? keyField = typeof(Poly1305).GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? accField = typeof(Poly1305).GetField("_acc", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(rField);
        Assert.IsNotNull(sField);
        Assert.IsNotNull(keyField);
        Assert.IsNotNull(accField);

        uint[] rBefore = ((uint[])rField.GetValue(poly)!).ToArray();
        uint[] sBefore = ((uint[])sField.GetValue(poly)!).ToArray();
        uint[] keyBefore = ((uint[])keyField.GetValue(poly)!).ToArray();

        Assert.IsTrue(Array.Exists(rBefore, v => v != 0), "Precondition: derived _r should be non-zero before Dispose.");
        Assert.IsTrue(Array.Exists(sBefore, v => v != 0), "Precondition: derived _s should be non-zero before Dispose.");
        Assert.IsTrue(Array.Exists(keyBefore, v => v != 0), "Precondition: derived _key should be non-zero before Dispose.");

        poly.Dispose();

        uint[] rAfter = (uint[])rField.GetValue(poly)!;
        uint[] sAfter = (uint[])sField.GetValue(poly)!;
        uint[] keyAfter = (uint[])keyField.GetValue(poly)!;
        uint[] accAfter = (uint[])accField.GetValue(poly)!;

        Assert.IsTrue(Array.TrueForAll(rAfter, v => v == 0), "Dispose must clear _r.");
        Assert.IsTrue(Array.TrueForAll(sAfter, v => v == 0), "Dispose must clear _s.");
        Assert.IsTrue(Array.TrueForAll(keyAfter, v => v == 0), "Dispose must clear _key.");
        Assert.IsTrue(Array.TrueForAll(accAfter, v => v == 0), "Dispose must clear _acc.");
    }

    /// <summary>
    /// Verifies that <c>Poly1305.ProcessFinalBlock</c> itself zeros the derived polynomial key schedule
    /// (<c>_r</c>, <c>_s</c>, <c>_key</c>) and the running accumulator <c>_acc</c> before returning the tag,
    /// independently of the framework's post-finalize auto-<see cref="HashAlgorithm.Initialize" />.
    /// </summary>
    /// <remarks>
    /// Both <see cref="HashAlgorithm.ComputeHash(byte[])" /> and
    /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> invoke
    /// <see cref="HashAlgorithm.Initialize" /> immediately after <c>HashFinal</c> returns, which on
    /// Poly1305 re-derives the schedule from the (already-cleared) <c>KeyValue</c>. That makes the end
    /// state observably all-zero even when <c>ProcessFinalBlock</c> clears nothing itself. To validate the
    /// in-window clearing this commit adds, the test invokes the protected <c>ProcessFinalBlock</c>
    /// directly via reflection — bypassing the framework's auto-Initialize so only the clearing performed
    /// by <c>ProcessFinalBlock</c> itself is observable.
    /// </remarks>
    [TestMethod]
    public void ComputeHash_ShouldClearDerivedKeyScheduleAndAccumulator()
    {
        using var poly = new Poly1305 { Key = (byte[])Poly1305TestKey.Clone() };
        byte[] message = System.Text.Encoding.ASCII.GetBytes("payload");

        // Drive ProcessBlock indirectly via HashCore (which accepts byte[]) to seed _acc with non-zero
        // state, then invoke the protected ProcessFinalBlock directly via reflection. The HashAlgorithm
        // pipeline (HashFinal -> Initialize) is bypassed, so the only thing that can zero _r/_s/_key/_acc
        // is ProcessFinalBlock itself.
        MethodInfo? hashCore = typeof(HashAlgorithm).GetMethod(
            "HashCore",
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(byte[]), typeof(int), typeof(int)]);
        MethodInfo? processFinalBlock = typeof(Poly1305).GetMethod("ProcessFinalBlock", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(hashCore);
        Assert.IsNotNull(processFinalBlock);

        hashCore.Invoke(poly, [message, 0, message.Length]);
        _ = processFinalBlock.Invoke(poly, null);

        FieldInfo? rField = typeof(Poly1305).GetField("_r", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? sField = typeof(Poly1305).GetField("_s", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? keyField = typeof(Poly1305).GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? accField = typeof(Poly1305).GetField("_acc", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(rField);
        Assert.IsNotNull(sField);
        Assert.IsNotNull(keyField);
        Assert.IsNotNull(accField);

        uint[] rAfter = (uint[])rField.GetValue(poly)!;
        uint[] sAfter = (uint[])sField.GetValue(poly)!;
        uint[] keyAfter = (uint[])keyField.GetValue(poly)!;
        uint[] accAfter = (uint[])accField.GetValue(poly)!;

        Assert.IsTrue(Array.TrueForAll(rAfter, v => v == 0), "ProcessFinalBlock must clear _r.");
        Assert.IsTrue(Array.TrueForAll(sAfter, v => v == 0), "ProcessFinalBlock must clear _s.");
        Assert.IsTrue(Array.TrueForAll(keyAfter, v => v == 0), "ProcessFinalBlock must clear _key.");
        Assert.IsTrue(Array.TrueForAll(accAfter, v => v == 0), "ProcessFinalBlock must clear _acc.");
    }

    /// <summary>
    /// Verifies that after a hash has been produced, explicitly reassigning <see cref="Poly1305.Key" /> with
    /// a fresh 32-byte key rebuilds the schedule and lets the same instance authenticate a second message
    /// correctly.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyIsReassignedBetweenMessages_ShouldAllowNewMessage()
    {
        byte[] message = System.Text.Encoding.ASCII.GetBytes("payload");

        using var poly = new Poly1305 { Key = (byte[])Poly1305TestKey.Clone() };
        byte[] firstTag = poly.ComputeHash(message);

        byte[] freshKey = new byte[32];
        RandomNumberGenerator.Fill(freshKey);
        poly.Key = freshKey;

        byte[] secondTag = poly.ComputeHash(message);

        Assert.AreEqual(16, firstTag.Length);
        Assert.AreEqual(16, secondTag.Length);
        CollectionAssert.AreNotEqual(firstTag, secondTag,
            "Re-keying with a different one-time key must change the produced tag.");
    }
}
