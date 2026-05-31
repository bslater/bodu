// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmFactoryTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="HashAlgorithmFactory" /> static helper which wraps a
/// builder delegate as an <see cref="IHashAlgorithmFactory{T}" /> instance.
/// </summary>
[TestClass]
public sealed class HashAlgorithmFactoryTests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithmFactory.From{T}(System.Func{T})" /> with a
    /// <see langword="null" /> builder throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name.
    /// </summary>
    [TestMethod]
    public void From_WhenBuilderIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashAlgorithmFactory.From<MD5>(null!);
        });

        Assert.AreEqual("builder", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmFactory.From{T}(System.Func{T})" /> with a non-null
    /// builder returns a non-null <see cref="DelegateHashAlgorithmFactory{T}" /> that produces a
    /// fresh instance on each call.
    /// </summary>
    [TestMethod]
    public void From_WhenBuilderProvided_ShouldReturnFactoryThatYieldsFreshInstancesOnCreate()
    {
        DelegateHashAlgorithmFactory<MD5> factory = HashAlgorithmFactory.From(() => MD5.Create());

        Assert.IsNotNull(factory);
        Assert.IsInstanceOfType<DelegateHashAlgorithmFactory<MD5>>(factory);

        using MD5 first = factory.Create();
        using MD5 second = factory.Create();

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);
    }

    /// <summary>
    /// Verifies that the factory invokes its builder delegate exactly once per call to
    /// <see cref="IHashAlgorithmFactory{T}.Create" />, propagating any configuration applied by the
    /// builder to the produced instance.
    /// </summary>
    [TestMethod]
    public void From_WhenBuilderProvided_ShouldInvokeBuilderOncePerCreate()
    {
        var callCount = 0;
        DelegateHashAlgorithmFactory<MD5> factory = HashAlgorithmFactory.From(() =>
        {
            callCount++;
            return MD5.Create();
        });

        using (MD5 _ = factory.Create()) { }
        using (MD5 _ = factory.Create()) { }
        using (MD5 _ = factory.Create()) { }

        Assert.AreEqual(3, callCount);
    }

    /// <summary>
    /// Verifies that an exception thrown by the builder propagates out of
    /// <see cref="IHashAlgorithmFactory{T}.Create" /> rather than being swallowed.
    /// </summary>
    [TestMethod]
    public void Create_WhenBuilderThrows_ShouldPropagateException()
    {
        DelegateHashAlgorithmFactory<MD5> factory = HashAlgorithmFactory.From<MD5>(() => throw new InvalidOperationException("boom"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = factory.Create();
        });
    }
}
