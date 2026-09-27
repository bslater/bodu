// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.Update.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GhashTests
{
    /// <summary>The RFC 8452 Appendix A worked example's key.</summary>
    private const string AppendixAKey = "25629347589242761d31f826ba4b757b";

    /// <summary>The RFC 8452 Appendix A worked example's two blocks, <c>X_1 || X_2</c>.</summary>
    private const string AppendixAData = "4f4f95668c83dfb6401762bb2d01a262d1a24ddd2721d006bbe45f20d3c9f362";

    /// <summary>
    /// Verifies that GHASH of RFC 8452 Appendix A's worked example, from a zero state, matches the published value.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenKeyIsGhash_ForRfc8452AppendixA_ShouldMatchPublishedValue(string kernel)
    {
        var key = Ghash.Key.ForGhash(Convert.FromHexString(AppendixAKey), Enum.Parse<Ghash.KernelKind>(kernel));
        byte[] state = new byte[16];

        Ghash.Update(in key, state, Convert.FromHexString(AppendixAData));

        Assert.AreEqual("bd9b3997046731fb96251b91f9c99d7a", Convert.ToHexString(state).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that POLYVAL of RFC 8452 Appendix A's worked example, from a zero state, matches the published value.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenKeyIsPolyval_ForRfc8452AppendixA_ShouldMatchPublishedValue(string kernel)
    {
        var key = Ghash.Key.ForPolyval(Convert.FromHexString(AppendixAKey), Enum.Parse<Ghash.KernelKind>(kernel));
        byte[] state = new byte[16];

        Ghash.Update(in key, state, Convert.FromHexString(AppendixAData));

        Assert.AreEqual("f7a3b47b846119fae5b7866cf5e5b77e", Convert.ToHexString(state).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that GHASH of NIST SP 800-38D's test case 2 inputs — the GCM specification's one-block example —
    /// matches the published <c>GHASH(H, A, C)</c>.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenKeyIsGhash_ForGcmSpecificationTestCase2_ShouldMatchPublishedValue(string kernel)
    {
        var key = Ghash.Key.ForGhash(Convert.FromHexString("66e94bd4ef8a2c3b884cfa59ca342b2e"), Enum.Parse<Ghash.KernelKind>(kernel));
        byte[] state = new byte[16];

        Ghash.Update(in key, state, Convert.FromHexString("0388dace60b6a392f328c2b971b2fe78"));
        Ghash.Update(in key, state, Convert.FromHexString("00000000000000000000000000000080"));

        Assert.AreEqual("f38cbb1ad69223dcc3457ae5b6b0f885", Convert.ToHexString(state).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that each kernel matches the bit-serial reference for GHASH at every data length from 0 to 200 bytes —
    /// every tail, and every position relative to a four-block group — from a non-zero state.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenKeyIsGhash_ShouldMatchBitSerialReferenceAtEveryLength(string kernel)
    {
        byte[] h = RandomBytes(16, 1);
        byte[] start = RandomBytes(16, 2);
        var key = Ghash.Key.ForGhash(h, Enum.Parse<Ghash.KernelKind>(kernel));

        for (int length = 0; length <= 200; length++)
        {
            byte[] data = RandomBytes(length, 100 + length);
            byte[] state = (byte[])start.Clone();

            Ghash.Update(in key, state, data);

            CollectionAssert.AreEqual(ReferenceGhash(h, start, data), state, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that each kernel matches the reference for POLYVAL at every data length from 0 to 200 bytes, from a
    /// non-zero state.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenKeyIsPolyval_ShouldMatchReferenceAtEveryLength(string kernel)
    {
        byte[] h = RandomBytes(16, 3);
        byte[] start = RandomBytes(16, 4);
        var key = Ghash.Key.ForPolyval(h, Enum.Parse<Ghash.KernelKind>(kernel));

        for (int length = 0; length <= 200; length++)
        {
            byte[] data = RandomBytes(length, 300 + length);
            byte[] state = (byte[])start.Clone();

            Ghash.Update(in key, state, data);

            CollectionAssert.AreEqual(ReferencePolyval(h, start, data), state, $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that each kernel agrees with every other on long runs under many keys, including keys and data with
    /// every bit set, where a lost carry or a wrong reduction term would show.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenRunIsLong_ShouldMatchBitSerialReference(string kernel)
    {
        var kind = Enum.Parse<Ghash.KernelKind>(kernel);
        byte[][] keys = [new byte[16], Enumerable.Repeat((byte)0xFF, 16).ToArray(), RandomBytes(16, 5), RandomBytes(16, 6)];
        byte[][] runs = [Enumerable.Repeat((byte)0xFF, 1000).ToArray(), RandomBytes(4096 + 48 + 7, 7)];

        foreach (byte[] h in keys)
        {
            foreach (byte[] data in runs)
            {
                var ghash = Ghash.Key.ForGhash(h, kind);
                byte[] state = new byte[16];
                Ghash.Update(in ghash, state, data);
                CollectionAssert.AreEqual(ReferenceGhash(h, new byte[16], data), state, $"GHASH key {Convert.ToHexString(h)}, {data.Length} bytes");

                var polyval = Ghash.Key.ForPolyval(h, kind);
                state = new byte[16];
                Ghash.Update(in polyval, state, data);
                CollectionAssert.AreEqual(ReferencePolyval(h, new byte[16], data), state, $"POLYVAL key {Convert.ToHexString(h)}, {data.Length} bytes");
            }
        }
    }

    /// <summary>
    /// Verifies that splitting whole-block data across calls produces the same state as one call, so a caller can feed
    /// a message in runs.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedKernels))]
    public void Update_WhenBlockAlignedDataIsSplitAcrossCalls_ShouldMatchOneCall(string kernel)
    {
        var key = Ghash.Key.ForGhash(RandomBytes(16, 8), Enum.Parse<Ghash.KernelKind>(kernel));
        byte[] data = RandomBytes(16 * 23, 9);
        byte[] whole = new byte[16];
        Ghash.Update(in key, whole, data);

        foreach (int split in new[] { 16, 48, 64, 80, 160, 352 })
        {
            byte[] state = new byte[16];
            Ghash.Update(in key, state, data.AsSpan(0, split));
            Ghash.Update(in key, state, data.AsSpan(split));

            CollectionAssert.AreEqual(whole, state, $"split at {split}");
        }
    }

    /// <summary>
    /// Verifies that a state that is not 16 bytes is rejected with <see cref="ArgumentException" /> naming the
    /// parameter.
    /// </summary>
    [TestMethod]
    public void Update_WhenStateIsNot16Bytes_ShouldThrowArgumentException()
    {
        var key = Ghash.Key.ForGhash(new byte[16], Ghash.KernelKind.Scalar);
        byte[] state = new byte[15];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ghash.Update(in key, state, new byte[16]);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
