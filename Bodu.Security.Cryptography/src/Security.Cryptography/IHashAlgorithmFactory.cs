// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IHashAlgorithmFactory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a factory that produces configured instances of a specific <see cref="HashAlgorithm"/> implementation.
/// </summary>
/// <typeparam name="T">The concrete type of <see cref="HashAlgorithm"/> this factory creates. Must inherit from <see cref="HashAlgorithm"/>.</typeparam>
/// <remarks>
/// <para>
/// This interface decouples the creation and configuration of hash algorithm instances from the logic that consumes them,
/// enabling reusable, testable, and extensible one-shot hash operations via the <see cref="HashAlgorithmHelper"/> utility class.
/// Implementations may return newly constructed or pooled algorithm instances depending on lifecycle management requirements.
/// </para>
/// <para>
/// <strong>How to obtain an instance.</strong> Most callers do not implement this interface directly. Use
/// <see cref="HashAlgorithmFactory.From{T}(System.Func{T})"/> to wrap an algorithm-construction delegate as
/// a <see cref="DelegateHashAlgorithmFactory{T}"/> — the right pick whenever the algorithm needs configuration
/// (a key, a round count, a variant flag) at the construction site. Implement
/// <see cref="IHashAlgorithmFactory{T}"/> directly only when adding instance pooling, telemetry, or other
/// cross-cutting behaviour around algorithm creation.
/// </para>
/// <para>
/// <strong>How it is consumed.</strong> Pass the factory to the static helpers on
/// <see cref="HashAlgorithmHelper"/>, <see cref="MerkleTreeHash"/>, or
/// <see cref="ParallelMerkleTreeHash"/>. Each call gets a fresh, fully configured algorithm instance — the
/// callers do not need to manage <see cref="System.IDisposable"/> lifecycles or thread-safety.
/// </para>
/// </remarks>
/// <seealso cref="HashAlgorithmFactory"/>
/// <seealso cref="DelegateHashAlgorithmFactory{T}"/>
/// <seealso cref="HashAlgorithmHelper"/>
public interface IHashAlgorithmFactory<out T>
    where T : System.Security.Cryptography.HashAlgorithm
{
    /// <summary>
    /// Creates and returns a new instance of the hash algorithm with any necessary configuration applied.
    /// </summary>
    /// <returns>A fully initialised <typeparamref name="T"/> instance, ready for data input.</returns>
    T Create();
}
