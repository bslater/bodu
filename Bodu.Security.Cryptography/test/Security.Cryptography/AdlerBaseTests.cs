// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public abstract partial class AdlerBaseTests<TTest, TAlgorithm, TVariant, TModulo>
        : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
        where TAlgorithm : AdlerBase<TModulo>, new()
        where TVariant : Enum
        where TModulo : unmanaged, INumber<TModulo>
    {
        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "modulo",
            ]);

            return list;
        }
    }
}