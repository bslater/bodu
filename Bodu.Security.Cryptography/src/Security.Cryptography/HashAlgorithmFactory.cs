// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides static factory helpers for constructing delegate-based implementations of
/// <see cref="IHashAlgorithmFactory{T}" />.
/// </summary>
/// <remarks>
/// <para>
/// This class simplifies creation of lightweight hash algorithm factories that encapsulate algorithm configuration
/// logic, particularly useful for one-shot hashing scenarios where consumers need to consistently configure algorithm
/// instances (e.g., setting a key, tuning rounds, or enabling specific variants).
/// </para>
/// <para>
/// The returned factory instances are typically passed into <see cref="HashAlgorithmHelper" /> methods to compute
/// hashes from spans, streams, or buffers in a memory-safe and reusable way. The same factory works with
/// <see cref="MerkleTreeHash" /> and <see cref="ParallelMerkleTreeHash" /> for tree-hashing pipelines that need a fresh
/// leaf-hash instance per node.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// 1. Bare SHA-256 factory.
/// IHashAlgorithmFactory<SHA256> sha256 = HashAlgorithmFactory.From(() => SHA256.Create());
/// byte[] digest = HashAlgorithmHelper.HashData(sha256, payload);
///
/// 2. Configured keyed-hash factory — applied consistently every time the consumer constructs an instance.
/// IHashAlgorithmFactory<SipHash64> sip = HashAlgorithmFactory.From(() => new SipHash64 { Key = sessionKey });
/// byte[] tag = HashAlgorithmHelper.HashData(sip, message);
///]]>
/// </code>
/// </example>
/// <seealso cref="IHashAlgorithmFactory{T}"/> <seealso cref="DelegateHashAlgorithmFactory{T}"/>
/// <seealso cref="HashAlgorithmHelper"/>
public static class HashAlgorithmFactory
{
    /// <summary>
    /// Creates a delegate-based factory that constructs new instances of the hash algorithm using the specified
    /// <paramref name="builder" />.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="HashAlgorithm" /> returned by the factory. Must be a concrete subclass.
    /// </typeparam>
    /// <param name="builder">
    /// A delegate that returns a fully configured instance of <typeparamref name="T" />. This delegate is invoked each
    /// time <see cref="IHashAlgorithmFactory{T}.Create" /> is called.
    /// </param>
    /// <returns>
    /// A delegate-backed implementation of <see cref="IHashAlgorithmFactory{T}" /> that produces new algorithm
    /// instances on demand.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public static DelegateHashAlgorithmFactory<T> From<T>(Func<T> builder)
        where T : System.Security.Cryptography.HashAlgorithm =>
        new(builder ?? throw new ArgumentNullException(nameof(builder)));
}
