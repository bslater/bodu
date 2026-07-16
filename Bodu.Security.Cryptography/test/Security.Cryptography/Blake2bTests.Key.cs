// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.Key.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    /// <summary>
    /// Verifies that reassigning <see cref="Blake2b.Key" /> zeroes the previously held key array so stale key
    /// material does not linger on the heap until the next garbage collection.
    /// </summary>
    [TestMethod]
    public void Key_WhenReassigned_ShouldZeroPreviousKeyArray()
    {
        byte[] firstKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

        using var sut = new Blake2b(512) { Key = firstKey };

        FieldInfo field = GetKeyValueField(sut.GetType());
        byte[] retainedFirstKey = (byte[])field.GetValue(sut)!;
        Assert.IsFalse(retainedFirstKey.All(b => b == 0), "Precondition: the first key must be non-zero.");

        sut.Key = new byte[32];

        CollectionAssert.AreEqual(
            new byte[retainedFirstKey.Length],
            retainedFirstKey,
            "Reassigning Key must zero the previously held key array.");
    }

    private static FieldInfo GetKeyValueField(Type type)
    {
        for (Type? t = type; t is not null; t = t.BaseType)
        {
            FieldInfo? field = t.GetField("KeyValue", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null)
                return field;
        }

        throw new InvalidOperationException("Expected an inherited 'KeyValue' field.");
    }

    /// <summary>
    /// Verifies that the keyed BLAKE2b-512 digest of an empty message with a 64-byte sequential key matches the
    /// known-answer value produced by Python's <c>hashlib.blake2b</c> reference implementation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInputAndFullKey_ShouldMatchKnownReferenceVector()
    {
        // Key: 64 sequential bytes 0x00..0x3f; input: empty.
        // Verified with: hashlib.blake2b(b'', key=bytes(range(64)), digest_size=64).hexdigest()
        byte[] key = Enumerable.Range(0, Blake2b.MaxKeySize / 8).Select(i => (byte)i).ToArray();
        const string expected = "10EBB67700B1868EFB4417987ACF4690AE9D972FB7A590C2F02871799AAA4786B5E996E8F0F4EB981FC214B005F42D2FF4233499391653DF7AEFCBC13FC51568";

        using var sut = new Blake2b(512) { Key = key };
        byte[] digest = sut.ComputeHash([]);

        Assert.AreEqual(expected, Convert.ToHexString(digest));
    }
}
