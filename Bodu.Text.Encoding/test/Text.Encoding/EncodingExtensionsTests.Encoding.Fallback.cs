// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.Fallback.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithExceptionFallbacks(System.Text.Encoding)" /> produces an
    /// encoding whose encoder and decoder both throw on invalid input.
    /// </summary>
    [TestMethod]
    public void WithExceptionFallbacks_WhenInvoked_ShouldReturnEncodingThatThrowsOnInvalidInput()
    {
        System.Text.Encoding strict = System.Text.Encoding.ASCII.WithExceptionFallbacks();

        Assert.IsInstanceOfType(strict.EncoderFallback, typeof(System.Text.EncoderExceptionFallback));
        Assert.IsInstanceOfType(strict.DecoderFallback, typeof(System.Text.DecoderExceptionFallback));
        Assert.ThrowsExactly<System.Text.EncoderFallbackException>(() =>
        {
            _ = strict.GetBytes("é");
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithExceptionFallbacks(System.Text.Encoding)" /> does not
    /// mutate the input encoding.
    /// </summary>
    [TestMethod]
    public void WithExceptionFallbacks_ShouldNotMutateInputEncoding()
    {
        var source = System.Text.Encoding.GetEncoding(
            20127,
            System.Text.EncoderFallback.ReplacementFallback,
            System.Text.DecoderFallback.ReplacementFallback);

        _ = source.WithExceptionFallbacks();

        Assert.IsInstanceOfType(source.EncoderFallback, typeof(System.Text.EncoderReplacementFallback));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithExceptionFallbacks(System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithExceptionFallbacks_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.WithExceptionFallbacks(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithReplacementFallbacks(System.Text.Encoding, string, string)" />
    /// produces an encoding whose encoder substitutes the supplied replacement string for invalid input.
    /// </summary>
    [TestMethod]
    public void WithReplacementFallbacks_WhenInvoked_ShouldReturnEncodingThatReplacesInvalidInput()
    {
        System.Text.Encoding replacing = System.Text.Encoding.ASCII.WithReplacementFallbacks("?", "?");

        var encoded = replacing.GetBytes("aéb");

        Assert.AreEqual("a?b", System.Text.Encoding.ASCII.GetString(encoded));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithReplacementFallbacks(System.Text.Encoding, string, string)" />
    /// throws <see cref="ArgumentException" /> when the encoder replacement cannot be encoded by the target
    /// encoding.
    /// </summary>
    [TestMethod]
    public void WithReplacementFallbacks_WhenEncoderReplacementIsUnencodable_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = System.Text.Encoding.ASCII.WithReplacementFallbacks("�", "?");
        });

        Assert.AreEqual("encoderReplacement", ex.ParamName);
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType(ex.InnerException, typeof(System.Text.EncoderFallbackException));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.WithReplacementFallbacks(System.Text.Encoding, string, string)" />
    /// throws <see cref="ArgumentNullException" /> when any argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithReplacementFallbacks_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = EncodingExtensions.WithReplacementFallbacks(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = System.Text.Encoding.UTF8.WithReplacementFallbacks(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = System.Text.Encoding.UTF8.WithReplacementFallbacks("?", null!));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.UsesExceptionFallbacks(System.Text.Encoding)" /> returns
    /// <see langword="true" /> for an encoding constructed with the exception fallbacks and
    /// <see langword="false" /> otherwise.
    /// </summary>
    [TestMethod]
    public void UsesExceptionFallbacks_ShouldReflectFallbackConfiguration()
    {
        System.Text.Encoding strict = System.Text.Encoding.UTF8.WithExceptionFallbacks();
        System.Text.Encoding replacing = System.Text.Encoding.UTF8.WithReplacementFallbacks();

        Assert.IsTrue(strict.UsesExceptionFallbacks());
        Assert.IsFalse(replacing.UsesExceptionFallbacks());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.UsesExceptionFallbacks(System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UsesExceptionFallbacks_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.UsesExceptionFallbacks(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.UsesReplacementFallbacks(System.Text.Encoding)" /> returns
    /// <see langword="true" /> for an encoding constructed with replacement fallbacks and
    /// <see langword="false" /> otherwise.
    /// </summary>
    [TestMethod]
    public void UsesReplacementFallbacks_ShouldReflectFallbackConfiguration()
    {
        System.Text.Encoding strict = System.Text.Encoding.UTF8.WithExceptionFallbacks();
        System.Text.Encoding replacing = System.Text.Encoding.UTF8.WithReplacementFallbacks();

        Assert.IsFalse(strict.UsesReplacementFallbacks());
        Assert.IsTrue(replacing.UsesReplacementFallbacks());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.UsesReplacementFallbacks(System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UsesReplacementFallbacks_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.UsesReplacementFallbacks(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
