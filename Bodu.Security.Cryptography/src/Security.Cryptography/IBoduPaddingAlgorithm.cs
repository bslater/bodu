// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBoduPaddingAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a symmetric cipher algorithm that supports the extended <see cref="BoduPaddingMode"/>
/// set, including <see cref="BoduPaddingMode.ISO7816_4"/>.
/// </summary>
/// <remarks>
/// <para>
/// The BCL <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> property accepts only
/// the five values defined by <see cref="System.Security.Cryptography.PaddingMode"/>. Setting it to a
/// value that represents <see cref="BoduPaddingMode.ISO7816_4"/> would throw. This interface provides
/// an alternative path: when <see cref="BoduPadding"/> is non-<see langword="null"/>, implementations
/// substitute it for <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> when
/// constructing <see cref="System.Security.Cryptography.ICryptoTransform"/> instances.
/// </para>
/// <para>
/// Setting <see cref="BoduPadding"/> to <see langword="null"/> reverts the algorithm to the value
/// of <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/>.
/// </para>
/// </remarks>
/// <seealso cref="BoduPaddingMode"/>
/// <seealso cref="PaddingFactory"/>
public interface IBoduPaddingAlgorithm
{
    /// <summary>
    /// Gets or sets the extended padding mode. When non-<see langword="null"/>, overrides
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> during transform creation.
    /// Set to <see langword="null"/> to revert to
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/>.
    /// </summary>
    /// <value>
    /// The <see cref="BoduPaddingMode"/> to use when creating transforms, or <see langword="null"/> to use
    /// the value of <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/>.
    /// </value>
    BoduPaddingMode? BoduPadding { get; set; }
}
