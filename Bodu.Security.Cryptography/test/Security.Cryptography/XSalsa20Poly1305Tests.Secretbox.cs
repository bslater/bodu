// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Tests.Secretbox.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class XSalsa20Poly1305Tests
{
    /// <summary>
    /// Verifies that <see cref="XSalsa20Poly1305.ToLibsodiumCombined" /> moves the trailing tag to the front, producing
    /// the libsodium <c>tag ‖ ciphertext</c> layout.
    /// </summary>
    [TestMethod]
    public void ToLibsodiumCombined_WhenGivenBoduLayout_ShouldEmitTagThenCiphertext()
    {
        var ciphertext = new byte[] { 1, 2, 3, 4, 5 };
        var tag = new byte[16];
        for (var i = 0; i < tag.Length; i++) tag[i] = (byte)(0xa0 + i);

        var boduLayout = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(boduLayout, 0);
        tag.CopyTo(boduLayout, ciphertext.Length);

        var libsodiumLayout = new byte[boduLayout.Length];
        XSalsa20Poly1305.ToLibsodiumCombined(boduLayout, libsodiumLayout);

        CollectionAssert.AreEqual(tag, libsodiumLayout.AsSpan(0, tag.Length).ToArray());
        CollectionAssert.AreEqual(ciphertext, libsodiumLayout.AsSpan(tag.Length).ToArray());
    }

    /// <summary>
    /// Verifies that converting Bodu layout to libsodium layout and back yields the original bytes, including when the
    /// conversion is performed in place.
    /// </summary>
    [TestMethod]
    public void ToFromLibsodiumCombined_WhenRoundTripped_ShouldBeIdentity()
    {
        var boduLayout = new byte[40 + 16];
        for (var i = 0; i < boduLayout.Length; i++) boduLayout[i] = (byte)(i * 3);
        var original = (byte[])boduLayout.Clone();

        // In-place: Bodu -> libsodium -> Bodu.
        XSalsa20Poly1305.ToLibsodiumCombined(boduLayout, boduLayout);
        XSalsa20Poly1305.FromLibsodiumCombined(boduLayout, boduLayout);

        CollectionAssert.AreEqual(original, boduLayout);
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20Poly1305.ToLibsodiumCombined" /> throws <see cref="ArgumentException" /> when the
    /// destination is too small.
    /// </summary>
    [TestMethod]
    public void ToLibsodiumCombined_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            XSalsa20Poly1305.ToLibsodiumCombined(new byte[20], new byte[19]);
        });
    }
}
