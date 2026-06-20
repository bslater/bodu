// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the inner stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using var stream = new HashingStream(null!, new Fnv1a32());
        });

        Assert.AreEqual("innerStream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the algorithm is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        using MemoryStream inner = new();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            using var stream = new HashingStream(inner, null!);
        });

        Assert.AreEqual("algorithm", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.CanRead" /> and <see cref="HashingStream.CanWrite" /> delegate to the
    /// inner stream while <see cref="HashingStream.CanSeek" /> is always <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerStreamIsMemoryStream_ShouldDelegateCapabilitiesExceptSeek()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        Assert.IsTrue(stream.CanRead);
        Assert.IsTrue(stream.CanWrite);
        Assert.IsFalse(stream.CanSeek);
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.Algorithm" /> exposes the algorithm instance supplied to the
    /// constructor.
    /// </summary>
    [TestMethod]
    public void Algorithm_WhenRead_ShouldReturnConstructorInstance()
    {
        using MemoryStream inner = new();
        Fnv1a32 algorithm = new();
        using HashingStream stream = new(inner, algorithm);

        Assert.AreSame(algorithm, stream.Algorithm);
    }
}
