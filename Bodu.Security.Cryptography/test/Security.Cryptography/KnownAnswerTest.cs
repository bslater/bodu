// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KnownAnswerTest.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed class KnownAnswerTest
{
    /// <summary>
    /// Gets or sets the expected output from the algorithm (hash or ciphertext).
    /// </summary>
    public byte[] ExpectedOutput { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the input message to be hashed or encrypted.
    /// </summary>
    public byte[] Input { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the name of the test case (e.g., "Empty Input", "ABC", "Block Size Input").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets optional parameters used for the test case (e.g., Key, Tweak, IV, Flags, Lengths).
    /// </summary>
    public IDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets an optional factory used to create a specifically configured
    /// <see cref="ICryptoTransform" /> for this known-answer test case.
    /// </summary>
    /// <remarks>
    /// When <see langword="null" />, the test harness should create the transform using its default
    /// algorithm setup. When provided, the delegate can apply test-specific configuration such as a
    /// custom key, IV, cipher mode, padding mode, or tweak before returning the transform to use.
    /// </remarks>
    public Func<IBlockCipher>? CipherFactory { get; init; }
}
