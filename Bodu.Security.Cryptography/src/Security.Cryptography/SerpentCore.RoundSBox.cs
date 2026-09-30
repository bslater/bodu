// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.RoundSBox.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class SerpentCore
{
    /// <summary>
    /// Supplies the S-box of a wide-block round and its inverse, so that a round written once against this interface is
    /// compiled once per S-box with the circuit inlined.
    /// </summary>
    /// <remarks>
    /// The wide-block rounds run eight at a time, one per S-box, as Serpent-128's do. Each names its S-box as a type
    /// argument, so the circuit is chosen when the round is compiled rather than by the round number as it runs.
    /// </remarks>
    internal interface IRoundSBox
    {
        /// <summary>
        /// Applies the S-box to four words in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        static abstract void Forward(ref uint x0, ref uint x1, ref uint x2, ref uint x3);

        /// <summary>
        /// Applies the inverse of the S-box to four words in bitsliced form.
        /// </summary>
        /// <param name="x0">The first word, replaced by the first output word.</param>
        /// <param name="x1">The second word, replaced by the second output word.</param>
        /// <param name="x2">The third word, replaced by the third output word.</param>
        /// <param name="x3">The fourth word, replaced by the fourth output word.</param>
        static abstract void Inverse(ref uint x0, ref uint x1, ref uint x2, ref uint x3);
    }
}
