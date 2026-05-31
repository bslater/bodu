// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IStreamCipherAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Marks a <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> that is an additive stream cipher rather than
/// a block cipher.
/// </summary>
/// <remarks>
/// <para>
/// Stream ciphers such as <see cref="ChaCha20" /> and <see cref="XChaCha20" /> derive from
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> for pipeline compatibility, but they have no cipher
/// block: they neither apply padding nor support block cipher modes of operation. This marker lets callers and test
/// harnesses distinguish them from genuine block ciphers without resorting to type-name checks — for example, to skip
/// padding-mode conformance suites that have no meaning for a stream cipher.
/// </para>
/// </remarks>
/// <seealso cref="IStreamCipher" />
public interface IStreamCipherAlgorithm
{
}
