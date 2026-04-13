// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the CityHash configuration under test.
    /// </summary>
    /// <remarks>
    /// CityHash algorithms have no configurable parameters — no key, no rounds, no seed. This enum satisfies the
    /// variant-parameterised base class contract and always resolves to a single <see cref="Default" /> configuration.
    /// </remarks>
    public enum CityHashVariant
    {
        /// <summary>The default (and only) variant — CityHash has no configurable parameters.</summary>
        Default
    }

    /// <summary>
    /// Provides the abstract test infrastructure shared across all <see cref="CityHash{T}" /> implementations.
    /// </summary>
    /// <typeparam name="TTest">The concrete test class, satisfying the CRTP pattern required by the base.</typeparam>
    /// <typeparam name="TAlgorithm">The concrete CityHash algorithm under test.</typeparam>
    public abstract partial class CityHashTests<TTest, TAlgorithm>
        : HashAlgorithmTests<TTest, TAlgorithm, CityHashVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, CityHashVariant>, new()
        where TAlgorithm : CityHash<TAlgorithm>, new()
    {
        /// <inheritdoc />
        public override IEnumerable<CityHashVariant> GetHashAlgorithmVariants() =>
            new[] { CityHashVariant.Default };

        /// <inheritdoc />
        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            return list;
        }
    }
}